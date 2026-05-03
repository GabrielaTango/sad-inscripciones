# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SAD Inscripciones is a full-stack membership and event registration system for the Sociedad Argentina de Diabetes (SAD). The system integrates with the SAD's Tango Gestión ERP (Axoft) via a one-way sync service.

Three codebases, one solution:
- `backend/SAD.Inscripciones.API/` — ASP.NET Core 8 Web API (MySQL)
- `frontend/` — React 19 + TypeScript + Vite (Tailwind CSS)
- `SyncService/` — .NET 8 Worker Service that syncs between the API's MySQL and Tango's SQL Server

## Build & Run

### Backend (from `backend/SAD.Inscripciones.API/`)
```bash
dotnet build
dotnet run                  # Listens on http://localhost:5161 (Swagger at /swagger)
```

### Frontend (from `frontend/`)
```bash
npm install
npm run dev                 # Vite dev server on port 80 (proxies /api → localhost:5161)
npm run build               # tsc -b + vite build
npm run lint                # ESLint
npm run preview
```

### SyncService (from `SyncService/`)
```bash
dotnet run                  # Background worker, polls on SyncIntervalMinutes (default 30 min)
```

### Docker (full stack)
```bash
docker-compose up           # MySQL + backend + nginx + ngrok tunnel
```

No test framework is configured in any project.

## Architecture

### Backend — Repository + Service pattern with Dapper

```
Controller → Service (interface) → Repository (interface) → DbConnectionFactory → MySQL
```

- **DbConnectionFactory** (`Data/DbConnectionFactory.cs`) returns a `MySqlConnection`. All SQL is raw Dapper — no EF Core.
- **Services vs. Repositories**: Repositories are thin SQL layers; services hold business logic (validation, orchestration, MercadoPago calls, etc.). Both are registered scoped in `Program.cs`; `MercadoPagoService` is singleton.
- **Exception handling**: `ExceptionHandlingMiddleware` is the first middleware. Throw from services; it maps to HTTP responses.
- **Schema migrations**: Hand-rolled SQL in `SAD.Inscripciones.API/SQL/Migration_*.sql`. Apply manually in order. `InitDatabase.sql` is the baseline.
- **Admin bootstrap**: `Program.cs` calls `usuarioService.SeedAdminAsync()` on startup.

### Auth — dual mode, JWT Bearer (8h)

`AuthController.Login` tries two paths in order:
1. **Internal admin**: `Usuarios` table, BCrypt-hashed passwords → JWT with role `Admin`.
2. **Socio (CUIT-based)**: requires `Usuario == Password` and a matching `Cuit` row in the `Clientes` table (populated from Tango's GVA14) → JWT without admin role.

Authorization policy `"Admin"` gates admin endpoints. The `cuit` claim identifies the socio on `/api/auth/socio-data`, `/api/resumen-cuenta`, etc.

### SyncService — bidirectional Tango ↔ MySQL bridge

The sync is one `Worker` loop with four phases per tick:

1. **Tango → MySQL (pull)**: Uses SQL Server **Change Tracking** on `GVA14` (clientes), `STA11` (artículos), `GVA18` (provincias), `GVA12` (comprobantes), `GVA07` (imputaciones). `ChangeTrackingService.Tables` is the source of truth for which tables are tracked and how their business keys are composed. Version cursors are persisted in a `SyncState` table on the Tango database. For each change, `Worker.UpsertAsync` reads the current row from Tango and POSTs to `/api/sync/{entity}` on the backend.
2. **MySQL → Tango (push inscripciones)**: Backend exposes `GET /api/sync/inscripciones` for confirmed-but-not-yet-synced registrations. `TangoInscripcionService` writes them into Tango (pedido / factura / recibo — see `InscripcionSync` section of `SyncService/appsettings.json` for talonario and vendor codes). Success → `PATCH /api/sync/inscripciones/{id}/tango` marks it synced.
3. **MySQL → Tango (push pagos)**: Same flow via `TangoPagoService`.
4. **Pending imputaciones**: `TangoImputacionService.EjecutarPendientesAsync` processes any deferred imputaciones.

Shared auth: both directions use the `X-Sync-Key` header (value from `SyncSettings:ApiKey` on backend, `ApiSettings:SyncKey` on SyncService). The backend-side `/api/sync/*` endpoints all call `ValidateApiKey()` — they are NOT JWT-protected.

Change Tracking setup SQL for the Tango DB lives at `scripts/sqlserver-sync-setup.sql` (creates `SyncQueue` + triggers as a legacy/alternative trigger-based path; the worker itself uses native CT via `ChangeTrackingService`).

### Frontend — React Router + Context auth

- **Routing**: Public pages (`/`, `/eventos`, `/inscripcion/:eventoId`, `/login`, etc.) + protected `/admin/*` tree wrapped in `<ProtectedRoute>` + `<AdminLayout>`. See `src/App.tsx` for the full map.
- **Auth state**: `context/AuthContext.tsx` persists token / cuit / isAdmin flag to localStorage (`sad_token`, `sad_cuit`, `sad_is_admin`). All API calls go through `services/api.ts`, which attaches the bearer token and redirects to `/login` on 401.
- **API base**: `services/api.ts` uses `/api` (relative). In dev, Vite proxies `/api` → `localhost:5161`. In prod, nginx handles the proxy.
- **Styling**: Tailwind CSS with a custom blue palette in `tailwind.config.js` (primary `#5D8AC8`, accent `#F5A623`). CSS variables drive border/ring/background tokens (shadcn-style). Not Bootstrap.
- **Service modules**: One per domain entity under `src/services/` — each wraps `api.ts` and returns typed results from `src/types/`.

## Cross-cutting Notes

- **CUIT is the universal socio identifier** — it lives in the JWT, in the `Clientes` table (mirrored from Tango's GVA14 via sync), and is what `ResumenCuentaController` joins on.
- **MercadoPago webhook**: `MercadoPagoWebhookController` receives async payment notifications → updates `Pagos` → the SyncService later pushes the payment to Tango.
- Two talonario / vendor config blocks live in `SyncService/appsettings.json` under `InscripcionSync` — these are Tango-specific IDs that must match the target Tango company database.
- The backend API URL `http://localhost:5161` is the dev default; ngrok URL `waspy-clarissa-elatedly.ngrok-free.dev` is whitelisted for CORS and used as the MercadoPago callback host in docker-compose.
- C# uses file-scoped namespaces and nullable reference types. TypeScript has strict mode on. Path alias `@/*` → `src/*` in the frontend.
