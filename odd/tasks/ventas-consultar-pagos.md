# Feature: ventas-consultar-pagos

## Objective
Let the admin see every product sale (not only paid ones) and reconcile pending sales against MercadoPago with one button, so a charged payment never stays without a registered sale.

## Problem / Why
Production bug (2026-10-08): a customer paid a product but the sale was not listed. Sales are created `Pendiente` before redirecting to MercadoPago and become `Pagada` only through the webhook or the buyer's return page. The webhook URL pointed to an ngrok dev host (`docker-compose.yml`, fixed in `3efadcf` on `fix/mp-webhook-ventas-url`), so buyers who did not return stayed `Pendiente`, and the admin list only showed `Pagada`. The webhook fix only covers new sales and is not infallible (server down while MercadoPago notifies), so a manual safety net is needed, which also recovers the already-paid old sales.

## Scope
- Admin sales list shows `Pendiente`, `Pagada` and `Impaga`, filterable by state.
- Button "Consultar pagos pendientes": queries MercadoPago for every `Pendiente` sale.
  - approved payment -> `Pagada` + confirmation email (existing flow);
  - no payment and sale younger than 24 h (from `FechaAlta`) -> stays `Pendiente`;
  - no payment and sale older than 24 h -> `Impaga`; it is not consulted again by the button.
- A sale with a payment still in progress at MercadoPago (e.g. `pending`, `in_process`, `authorized`) is never marked `Impaga`.
- An approved payment arriving later (webhook or buyer return) confirms the sale even if it is `Impaga`.

## Decisions (user, 2026-10-08)
- No expiration on the MercadoPago preference.
- Threshold: 24 hours since `FechaAlta`.
- No new columns. `FechaPago` keeps being the confirmation moment (so recovered old sales show the consultation date, accepted). For `Impaga`, the consultation moment is the existing `UpdatedAt`; `FechaPago` stays NULL.
- The button consults all pending sales in one action (not per row).
- Supersedes the earlier decision in `odd/tasks/productos-venta.md` that the admin list shows only `Pagada`.
- Implemented without SDD (user choice).

## Constraints
- Do NOT modify the eventos/inscripciones circuit (controllers, services, repositories, webhook, `CrearPreferenciaAsync`, `ExternalReferenceHelper`).
- One failing sale during the bulk consultation must not abort the rest.
- Price/amount checks of the existing confirmation stay as they are.

## Checks
- TDD: off (source: CLAUDE.md, "No test framework is configured in any project").
- Backend: `dotnet build` in `backend/SAD.Inscripciones.API`.
- Frontend: `npm run build` in `frontend`; `npx eslint` on touched files.
- Manual: local sandbox purchase without returning to the site, then the button.
- RDD: off (global) -> ordinary checks only.

## Delivery
- Branch: `feat/ventas-consultar-pagos`, created on top of `fix/mp-webhook-ventas-url`. Strategy: `single-pr`. Forecast ~350 authored lines.

## Tasks
- [x] T1 (commits `59b9d04`, `b54fc7b`; manual local check confirmed by the user on 2026-10-08: paid-without-returning sale became `Pagada`, old unpaid ones `Impaga`) Backend + frontend in one work unit. Route: delegated writer (2+ non-trivial files).
  - Repository: confirmation accepts `Pendiente` and `Impaga`; list pending sales; mark `Impaga` (only from `Pendiente`); date-range filter usable for unpaid sales.
  - Service: bulk consultation returning a summary (consulted, paid, unpaid, still pending, errors); `VerificarAsync` also checks MercadoPago for `Impaga` sales.
  - Controller: Admin `POST api/ventas-producto/consultar-pendientes`; list endpoint returns all states by default.
  - Migration only if the `Estado` column type does not accept `Impaga`.
  - Admin page: state filter and column, creation date, consultation date for `Impaga`, button with result summary; totals count only `Pagada`. Excel export includes the state.

## Progress
- Feature document created; branch created.
- T1 done (checkbox above stays open until the manual check): commit `59b9d04` (delegated writer, ~270 lines). No migration: `VentasProducto.Estado` is `VARCHAR(20)`. Age check runs in SQL against `UTC_TIMESTAMP()`, the same clock that writes `FechaAlta`. `HorasHastaImpaga = 24` in `VentaProductoService`.
- Parent fix `b54fc7b` (inline, 3 files): the shared `BuscarTodosPagosPorReferenciaAsync` returns an empty list on any MercadoPago error, which the bulk run would read as "no payment". Added `BuscarPagosPorReferenciaEstrictoAsync` (throws) and used it only in `ConsultarPendientesAsync`; the shared method is untouched.
- Checks: `dotnet build` 0 errors, 1 pre-existing warning (parent and verifier, built to a temp output because a running backend locks `bin`); `npm run build` OK; eslint clean on the 3 touched frontend files (writer and verifier).
- Risk assessment: unavailable (`gentle-ai review assess` crashed, then refused on untracked files) -> treated as high -> independent verifier: 8/8 requirement checks PASS, no blocking defect.
- Not verified: nothing was run against MySQL or MercadoPago; the UI was not exercised in a browser.

## Known limitations (accepted or pre-existing, not fixed)
- The bulk run makes one sequential MercadoPago request per pending sale inside one HTTP request; with a large backlog the proxy may time out while the server keeps working. Re-clicking is idempotent.
- Dates are stored in UTC and serialized without `Z`, so the admin page shows them offset by the local UTC offset (pre-existing for `FechaPago`, now also "Fecha alta" and "Consultada").
- A sale whose only payments are rejected/cancelled becomes `Impaga` after 24 h (follows the rule as decided).

## Next step
Feature complete. `fix/mp-webhook-ventas-url` is already merged to `main` (PR #5). Remaining, user's decision: push this branch, PR to `main`, deploy on the VPS, then use the button there to recover paid sales left `Pendiente`.
