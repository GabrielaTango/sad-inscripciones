# Feature: productos-venta

## Objective
Standalone module to sell products (e.g. a t-shirt) with MercadoPago payment, fully separate from the eventos/inscripciones circuit and never synced to Tango.

## Problem / Why
SAD needs to sell merchandise. Buyers provide DNI, first name, last name, email and product-specific extra fields (e.g. size). A sale counts only when MercadoPago approves the payment; then a per-product confirmation email is sent.

## Scope
- Admin CRUD of products (name, description, price, active, image URL, dynamic extra-field schema, email subject/body).
- Public purchase form -> pending sale -> MercadoPago preference -> redirect.
- Dedicated webhook confirming the payment idempotently and sending the email.
- Admin list of paid sales with filters and export.

## Constraints
- Do NOT modify the eventos/inscripciones circuit (controllers, services, repositories, webhook, `CrearPreferenciaAsync`, `ExternalReferenceHelper`).
- No Tango sync.
- Price comes from the product, never from the buyer.
- Dynamic fields stored as JSON in MEDIUMTEXT columns (project precedent: `PagosCuentaCorriente.Comprobantes`, `EmailTemplates.BodyJson`).
- Confirmation email = plain subject/body fields on each product with `{{Var}}` replacement (not Unlayer templates).
- Uncommitted inscripciones changes in the working tree are NOT part of this feature: never stage them.

## Design
- Tables: `Productos` (Id, Nombre, Descripcion, Precio, Activo, ImagenUrl, CamposExtra MEDIUMTEXT, MailAsunto, MailCuerpoHtml, FechaAlta) and `VentasProducto` (Id, ProductoId, PublicRef, Dni, Nombre, Apellido, Email, DatosExtra MEDIUMTEXT, Importe, Estado Pendiente/Pagada/Rechazada, MpPaymentId UNIQUE, FechaAlta, FechaPago, MailEnviado).
- `CamposExtra` schema: `[{ "key", "label", "type": "text|number|select", "options": [], "required" }]`.
- External reference: `venta-{ventaId}-{publicRef}`. The existing webhook ignores it (int parse of "venta" fails).
- New `MercadoPagoService.CrearPreferenciaVentaAsync` sets its own `NotificationUrl` -> `POST /api/webhooks/mercadopago/ventas`.
- New `EmailService.EnviarConfirmacionVentaAsync` reusing SMTP config and `{{Var}}` replacement. Variables: Nombre, Apellido, Dni, Email, Producto, Importe, plus every extra-field key.

## Checks
- TDD: off (source: CLAUDE.md, "No test framework is configured in any project").
- Backend: `dotnet build` in `backend/SAD.Inscripciones.API`.
- Frontend: `npm run build` and `npm run lint` in `frontend`.
- Manual: MercadoPago sandbox purchase end to end.
- RDD: off (global) -> ordinary checks only.

## Delivery
- Branch: `feat/productos-venta`. Strategy: `single-pr` (user decision: solo developer, no reviewer). Forecast ~2000-2500 authored lines; one work-unit commit per task keeps it reviewable commit by commit.

## Tasks
- [x] T1 SQL migration `Migration_ProductosVenta.sql`, models, repositories (Productos, VentasProducto) + DI registration.
- [x] T2 Products CRUD: service, DTOs, `ProductosController` (public GET active, Admin POST/PUT/DELETE); admin pages list + form with dynamic-field and email editor; sidebar entry.
- [x] T3 Pending sale + MP preference (`CrearPreferenciaVentaAsync`) + `POST /api/ventas-producto`; public purchase page `/productos/:id/comprar` rendering dynamic fields. Commit: TBD.
- [ ] T4 `VentaProductoWebhookController` with idempotent confirmation (row lock, amount check, UNIQUE MpPaymentId) + public result page `/productos/pago/resultado`.
- [ ] T5 Confirmation email `EnviarConfirmacionVentaAsync` triggered only on the Pendiente -> Pagada transition.
- [ ] T6 Admin paid-sales list with product filter, extra-field columns and export.

## Progress
- Branch created from `main`.
- T1 done: `SQL/Migration_ProductosVenta.sql`, `Models/{Producto,VentaProducto,CampoExtraProducto}.cs`, `Repositories/{ProductoRepository,VentaProductoRepository}.cs` + interfaces, DI registered in `Program.cs`. `dotnet build` succeeds (0 errors, re-run by parent). Commit: `ed46203` (~475 lines incl. this doc).
- Decision (user-approved): a rejected MercadoPago payment does NOT change the sale state; it is only logged and the sale stays `Pendiente`. Reason: MP lets the buyer retry on the same preference, producing a later approved payment with the same external_reference that must still confirm the sale. T4: webhook must not call `MarcarRechazadaAsync` (remove it from the repository if unused).

- T2 done: `DTOs/{ProductoCreateDto,ProductoUpdateDto,ProductoDto}.cs` (ProductoDto full + ProductoPublicoDto without mail fields), `Services/{Interfaces/IProductoService,ProductoService}.cs` (CamposExtra JSON serialize/deserialize, key normalization + reserved-key/type/select-options validation), `Controllers/ProductosController.cs` (`api/productos` GET/`{id}` public active-only, `admin`/`admin/{id}` full DTO, POST/PUT/DELETE Admin), DI registered in `Program.cs`. Frontend: `types/models.ts` (Producto/CampoExtraProducto/ProductoPublico/ProductoForm), `services/productosService.ts`, `pages/admin/{ProductosAdminPage,ProductoDetallePage}.tsx` (list + full-page form with extra-field editor and email variables help box), routes + "Productos" sidebar entry (Package icon) in `App.tsx`/`AdminLayout.tsx`. `dotnet build`: 0 errors. `npm run build`: OK. `npm run lint`: 0 errors in touched files (6 pre-existing errors elsewhere, unrelated). Commit: `4ea5436` (~720 lines; lint on touched files re-run by parent: clean).

- T3 done: `Services/VentaExternalReference.cs` (Build/TryParseVentaId, `venta-{id}-{publicRef}` prefix so the inscripciones webhook parse fails and ignores it), `IMercadoPagoService.CrearPreferenciaVentaAsync` + `MercadoPagoService` impl (own item/title/BackUrls under `/productos/pago/resultado`, `AutoReturn=approved`, optional `NotificationUrl` from new `MercadoPago:BackendPublicUrl` config key — only set when non-empty), `DTOs/VentaProductoCreateDto.cs` (+ result DTO), `Services/{Interfaces/IVentaProductoService,VentaProductoService}.cs` (loads active product, validates Dni 7-8 digits/Nombre/Apellido/Email, validates DatosExtra against CamposExtra dropping unknown keys, Importe from product only, PublicRef = `Guid.NewGuid().ToString("N")` mirroring `InscripcionService`), `Controllers/VentasProductoController.cs` (`POST api/ventas-producto`, anonymous), DI registered in `Program.cs`. `appsettings.json`: added `MercadoPago:BackendPublicUrl` (empty). `docker-compose.yml`: added `MercadoPago__BackendPublicUrl` env var pointing at the existing ngrok host (pattern existed via `MercadoPago__FrontendBaseUrl`). Frontend: `types/models.ts` (`VentaProductoCreateForm`/`VentaProductoCreateResult`), `services/ventasProductoService.ts`, `pages/ComprarProductoPage.tsx` (public form: DNI/Nombre/Apellido/Email + dynamic CamposExtra inputs, client-side validation mirroring backend, submit redirects via `window.location.href = result.initPoint`), route `/productos/:id/comprar` in `App.tsx`. Decision: used `preference.InitPoint` (not SandboxInitPoint), matching the existing inscripcion flow. `dotnet build`: 0 errors (1 pre-existing unrelated warning). `npm run build`: OK. `npx eslint` on touched files (`ComprarProductoPage.tsx`, `ventasProductoService.ts`, `types/models.ts`, `App.tsx`): 0 errors. Commit: TBD (not committed by this task per instructions — left for user/parent).

## Next step
T4 (webhook + result page).
