# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SAD Inscripciones is a full-stack membership registration and event management system for the Sociedad Argentina de Diabetes (SAD). It consists of a C# .NET 8 backend API and a React + TypeScript frontend.

## Build & Run Commands

### Backend (from `backend-sad-inscripciones/SAD.Inscripciones.API/`)
```bash
dotnet build                # Build the project
dotnet run                  # Run the API (port 5161)
```

### Frontend (from `Frontend/`)
```bash
npm install                 # Install dependencies
npm run dev                 # Start Vite dev server (localhost:5173)
npm run build               # TypeScript check + production build
npm run lint                # Run ESLint
npm run preview             # Preview production build
```

No test framework is currently configured for either frontend or backend.

## Architecture

### Backend — ASP.NET Core 8 Web API

Uses the **Repository pattern** with **Dapper** (micro-ORM) for direct SQL against SQL Server.

```
Controllers → DTOs → Models → Repositories → DbConnectionFactory → SQL Server
```

- **Controllers**: `AuthController`, `InscripcionesController`, `EventosController`, `ContactoController`
- **Repositories**: Interface + implementation per entity, injected via DI in `Program.cs`
- **Auth**: JWT Bearer tokens (8h expiry). CUIT validated against external GVA14 table (Tango accounting system). Only event management endpoints are protected with `[Authorize]`.
- **Database**: SQL Server with tables `Inscripciones`, `Eventos`, `Contactos`. Schema in `SQL/InitDatabase.sql`.

### Frontend — React 19 + TypeScript + Vite

- **Routing**: React Router DOM with pages at `/`, `/nosotros`, `/eventos`, `/inscripcion`, `/contacto`, `/login`
- **Auth state**: React Context (`AuthContext`) stores JWT token and CUIT in localStorage (`sad_token`, `sad_cuit` keys)
- **Styling**: Bootstrap 5 with custom CSS variables (`--sad-primary: #1a5276`, `--sad-secondary: #2e86c1`, `--sad-accent: #48c9b0`) defined in `index.css`
- **Page components**: Named with `Page` suffix (e.g., `LoginPage.tsx`)

### Key Dependencies

**Backend (NuGet):** Dapper, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.Data.SqlClient, Swashbuckle
**Frontend (npm):** react 19, react-router-dom 7, bootstrap 5, vite 7, typescript 5.9

## Development Notes

- CORS is configured for `http://localhost:5173` only — the frontend dev server
- Backend API base URL is hardcoded in frontend fetch calls as `http://localhost:5161`
- The `EventosPage` currently uses a hardcoded events list instead of fetching from the API
- C# uses file-scoped namespaces and nullable reference types
- TypeScript strict mode is enabled
