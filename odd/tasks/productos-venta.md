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
- [x] T3 Pending sale + MP preference (`CrearPreferenciaVentaAsync`) + `POST /api/ventas-producto`; public purchase page `/productos/:id/comprar` rendering dynamic fields. Commit: `fee3e82`.
- [x] T4 `VentaProductoWebhookController` with idempotent confirmation (row lock, amount check, UNIQUE MpPaymentId) + public result page `/productos/pago/resultado`.
- [x] T5 Confirmation email `EnviarConfirmacionVentaAsync` triggered only on the Pendiente -> Pagada transition.
- [x] T6 Admin paid-sales list with product filter, extra-field columns and export.

## Progress
- Branch created from `main`.
- T1 done: `SQL/Migration_ProductosVenta.sql`, `Models/{Producto,VentaProducto,CampoExtraProducto}.cs`, `Repositories/{ProductoRepository,VentaProductoRepository}.cs` + interfaces, DI registered in `Program.cs`. `dotnet build` succeeds (0 errors, re-run by parent). Commit: `ed46203` (~475 lines incl. this doc).
- Decision (user-approved): a rejected MercadoPago payment does NOT change the sale state; it is only logged and the sale stays `Pendiente`. Reason: MP lets the buyer retry on the same preference, producing a later approved payment with the same external_reference that must still confirm the sale. T4: webhook must not call `MarcarRechazadaAsync` (remove it from the repository if unused).

- T2 done: `DTOs/{ProductoCreateDto,ProductoUpdateDto,ProductoDto}.cs` (ProductoDto full + ProductoPublicoDto without mail fields), `Services/{Interfaces/IProductoService,ProductoService}.cs` (CamposExtra JSON serialize/deserialize, key normalization + reserved-key/type/select-options validation), `Controllers/ProductosController.cs` (`api/productos` GET/`{id}` public active-only, `admin`/`admin/{id}` full DTO, POST/PUT/DELETE Admin), DI registered in `Program.cs`. Frontend: `types/models.ts` (Producto/CampoExtraProducto/ProductoPublico/ProductoForm), `services/productosService.ts`, `pages/admin/{ProductosAdminPage,ProductoDetallePage}.tsx` (list + full-page form with extra-field editor and email variables help box), routes + "Productos" sidebar entry (Package icon) in `App.tsx`/`AdminLayout.tsx`. `dotnet build`: 0 errors. `npm run build`: OK. `npm run lint`: 0 errors in touched files (6 pre-existing errors elsewhere, unrelated). Commit: `4ea5436` (~720 lines; lint on touched files re-run by parent: clean).

- T3 done: `Services/VentaExternalReference.cs` (Build/TryParseVentaId, `venta-{id}-{publicRef}` prefix so the inscripciones webhook parse fails and ignores it), `IMercadoPagoService.CrearPreferenciaVentaAsync` + `MercadoPagoService` impl (own item/title/BackUrls under `/productos/pago/resultado`, `AutoReturn=approved`, optional `NotificationUrl` from new `MercadoPago:BackendPublicUrl` config key — only set when non-empty), `DTOs/VentaProductoCreateDto.cs` (+ result DTO), `Services/{Interfaces/IVentaProductoService,VentaProductoService}.cs` (loads active product, validates Dni 7-8 digits/Nombre/Apellido/Email, validates DatosExtra against CamposExtra dropping unknown keys, Importe from product only, PublicRef = `Guid.NewGuid().ToString("N")` mirroring `InscripcionService`), `Controllers/VentasProductoController.cs` (`POST api/ventas-producto`, anonymous), DI registered in `Program.cs`. `appsettings.json`: added `MercadoPago:BackendPublicUrl` (empty). `docker-compose.yml`: added `MercadoPago__BackendPublicUrl` env var pointing at the existing ngrok host (pattern existed via `MercadoPago__FrontendBaseUrl`). Frontend: `types/models.ts` (`VentaProductoCreateForm`/`VentaProductoCreateResult`), `services/ventasProductoService.ts`, `pages/ComprarProductoPage.tsx` (public form: DNI/Nombre/Apellido/Email + dynamic CamposExtra inputs, client-side validation mirroring backend, submit redirects via `window.location.href = result.initPoint`), route `/productos/:id/comprar` in `App.tsx`. Decision: used `preference.InitPoint` (not SandboxInitPoint), matching the existing inscripcion flow. `dotnet build`: 0 errors (1 pre-existing unrelated warning). `npm run build`: OK. `npx eslint` on touched files (`ComprarProductoPage.tsx`, `ventasProductoService.ts`, `types/models.ts`, `App.tsx`): 0 errors. Commit: `fee3e82` (~510 lines; backend build re-run by parent: 0 errors).

- T4 done: `Services/VentaExternalReference.cs` (`TryParseVentaId` → `TryParse` now also extracts `publicRef`, so the caller can verify it against the loaded sale), `Repositories/{IVentaProductoRepository,VentaProductoRepository}.cs` (removed unused `MarcarRechazadaAsync` + its interface member, per the user-approved decision; tidied the now-inaccurate "or Rechazada" comments on `AlreadyPaid`), `Services/{Interfaces/IVentaProductoService,VentaProductoService}.cs` (`ProcesarPagoAsync(MercadoPagoPaymentInfo)` shared entry point: ignores non-venta references, verifies PublicRef match, only "approved" confirms via `ConfirmarPagoAsync`, everything else just logs and leaves Pendiente; `VerificarAsync(publicRef)` used by the result page — if still Pendiente, searches MP by external reference and replays `ProcesarPagoAsync` for each approved payment, then returns `VentaProductoEstadoDto`), `DTOs/VentaProductoEstadoDto.cs` (Estado/ProductoId/ProductoNombre/Nombre/Importe only — no Email/Dni/DatosExtra), `Controllers/VentaProductoWebhookController.cs` (`api/webhooks/mercadopago/ventas`, anonymous, same notification parsing as the existing inscripciones webhook, 200 on handled/ignored, 500 only on unexpected exceptions for MP retry), `Controllers/VentasProductoController.cs` (+`POST {publicRef}/verificar`, anonymous). Frontend: `types/models.ts` (`VentaProductoEstado`), `services/ventasProductoService.ts` (+`verificar`), `pages/ProductoPagoResultadoPage.tsx` (public route `/productos/pago/resultado`; extracts `publicRef` from `venta-{id}-{publicRef}`, calls `verificar`, shows approved/pending/rejected UI in Spanish mirroring `PagoResultadoPage`, rejected offers "Volver a intentar" back to `/productos/{productoId}/comprar` using `ProductoId` from the status DTO), route added in `App.tsx`. `dotnet build`: 0 errors (1 pre-existing unrelated warning in `EventoPrecioService.cs`). `npm run build`: OK. `npx eslint` on touched files (`ProductoPagoResultadoPage.tsx`, `ventasProductoService.ts`, `types/models.ts`, `App.tsx`): 0 errors. Commit: `4adf9ae`.

- T5 done: `Services/EmailService.cs` (+`EnviarConfirmacionVentaAsync(VentaProducto, Producto)`: reuses `GetConfigAsync`/`SendAsync`/`ReplaceVariables` unchanged; skips + logs if email inactive, venta has no Email, or the producto has no `MailAsunto`/`MailCuerpoHtml`; builds variables Nombre/Apellido/Dni/Email/Producto/Importe (same `"C2"` es-AR format as `BuildVariables`) plus one entry per `DatosExtra` key, all HTML-encoded since these are buyer-typed values; swallows and logs exceptions, returns bool), `Services/Interfaces/IEmailService.cs` (+ method doc). `Services/{Interfaces/IVentaProductoService is unchanged, VentaProductoService}.cs`: added `IEmailService` dependency and a private `EnviarMailConfirmacionAsync(venta)` helper called only from the `ConfirmarPagoResult.Confirmed` branch of `ProcesarPagoAsync` (never on `AlreadyPaid`/idempotent replays); on successful send calls `MarcarMailEnviadoAsync`. No existing `EmailService`/`MercadoPagoService` method behavior changed. Frontend: verified `ProductoDetallePage.tsx`'s `disponibles` help-text array (`Nombre, Apellido, Dni, Email, Producto, Importe` + extra-field keys) already matches exactly — no change needed. `dotnet build`: 0 errors (same 1 pre-existing unrelated warning in `EventoPrecioService.cs`). `npm run build`: OK (no frontend files changed). `npx eslint` on `ProductoDetallePage.tsx`: 0 errors. Commit: `1edb400`.

- T6 done: `Models/VentaProductoAdminRow.cs` (repo-level projection: ProductoNombre from JOIN, DatosExtra kept as raw JSON), `DTOs/VentaProductoAdminDto.cs` (DatosExtra deserialized to Dictionary<string,string>), `Repositories/{IVentaProductoRepository,VentaProductoRepository}.cs` (`ListAsync` extended to `(productoId, estado, desde, hasta, texto)`, parameterized SQL with `JOIN Productos`, `FechaPago` range with exclusive day-after upper bound, `LIKE` texto search over Dni/Nombre/Apellido/Email), `Services/{Interfaces/IVentaProductoService,VentaProductoService}.cs` (+`ListAdminAsync` mapping rows to the DTO, +`ExportToExcelAsync` reusing `ClosedXML.Excel` exactly like `InscripcionService`/`BecaCodigoService`: columns are the filtered product's `CamposExtra` labels in order, or the union of `DatosExtra` keys in first-seen order when no product filter), `Controllers/VentasProductoController.cs` (+`GET api/ventas-producto` and `GET api/ventas-producto/export`, both `[Authorize(Policy="Admin")]`; `estado` missing → defaults to "Pagada", "Todas" (case-insensitive) → no estado filter — kept the existing anonymous POST endpoints untouched). Frontend: `types/models.ts` (`VentaProductoAdmin`, `VentaProductoAdminFiltros`), `services/ventasProductoService.ts` (+`listAdmin`, +`exportExcel` — same fetch-blob-with-bearer-token pattern as `inscripcionesService.exportExcel`), `pages/admin/VentasProductoAdminPage.tsx` (route `/admin/productos/ventas`: product/estado/date-range/text filters, debounced reload, extra-field columns from the selected product's `CamposExtra` or a compact "clave: valor" cell otherwise, total count + total importe, export button), `pages/admin/ProductosAdminPage.tsx` (+"Ver ventas" row action navigating to `?productoId=`, +"Copiar link de compra" row action copying `${origin}/productos/{id}/comprar`), `App.tsx` (imported the page, route `productos/ventas` registered before `productos/:id`), `components/Admin/AdminLayout.tsx` (+"Ventas productos" sidebar entry right after "Productos"; added `end: true` to "Productos" so it doesn't stay highlighted on the ventas subpage). `dotnet build`: 0 errors (same 1 pre-existing unrelated warning in `EventoPrecioService.cs`). `npm run build`: OK. `npx eslint` on all touched frontend files: 0 errors. Commit: `66ba908`.

## Next step
Feature complete (T1-T6). Manual end-to-end test in MercadoPago sandbox + apply `SQL/Migration_ProductosVenta.sql`.

## Post-implementation fixes (from manual testing)
- `92aa509` select options input keeps raw text (commas typable); admin sales list shows only `Pagada` (estado filter/column removed).
- `dc178a4`, `4998ef0` Pago Fácil and Rapipago excluded from product checkout; return page shows "Compra realizada" confirmation with buyer, amount and MP payment reference.
- Decision: no "resend email" action (user declined). Mail is sent once on Pendiente -> Pagada; if SMTP is inactive/failing at that moment, it is not retried.
- Pending (user decision, not applied): `docker-compose.yml` has `MercadoPago__BackendPublicUrl` pointing at the ngrok host; production should use `https://www.diabetes2.org.ar`. `docker-compose.ngrok.yml` lacks the key.

## T7 Public product catalog (added 2026-09-30)
Decision: products are shown separately from eventos (no shared list, card or service); only site chrome (Navbar, Footer, HomePage) is touched.

## T8 Product image stored in the database (added 2026-10-05)
Decision (user): the image is uploaded and stored in MySQL instead of a hand-typed URL. Supersedes "image URL" in Scope and `ImagenUrl` in Design.
- Table `ProductoImagenes` (ProductoId PK/FK, Contenido MEDIUMBLOB, ContentType, UpdatedAt) in a new `SQL/Migration_ProductosImagen.sql`, which also drops `Productos.ImagenUrl`. Separate table so product listings never load the blob.
- `GET api/productos/{id}/imagen` anonymous, binary response with cache headers; `PUT`/`DELETE api/productos/admin/{id}/imagen` Admin. Max 2 MB; PNG/JPEG/WebP only (no SVG: served same-origin).
- Response DTOs keep `imagenUrl`, now computed (`/api/productos/{id}/imagen?v=<UpdatedAt ticks>` or null); removed from create/update DTOs.
- Admin form: file picker with preview replaces the URL text input.
- [x] T8 Route: delegated writer (2+ non-trivial files). Checks: `dotnet build`, `npm run build`, `npx eslint` on touched files. Done: `dotnet build` 0 errors (re-run by parent; same pre-existing `EventoPrecioService.cs` warning), `npm run build` OK, eslint clean on touched files (both writer-reported). Risk assessment: medium (RDD off -> writer self-verification + parent readback). Commit: `0582a72` (~270 lines).
- Pending (manual): apply `SQL/Migration_ProductosImagen.sql` (drops `Productos.ImagenUrl`; existing URLs are lost) and test upload/replace/remove in the admin form. Not exercised against a running server or database.
- Notes: upload validation trusts the client-declared content type (admin-only endpoint); the public image endpoint also serves images of inactive products; `?v=` has second precision.

- [x] T7 `/productos` catalog page + `ProductoCard`; "Productos" link in Navbar (desktop + mobile) and Footer; "Volver a productos" link on purchase page; HomePage "Productos" section below "Próximos Eventos" (max 3, own fetch/loading, hidden if none or on error, "Ver todos los productos" button). Route: delegated writer (4+ files). Checks: `npm run build`, `npx eslint` on touched files. Done: build OK, eslint clean on touched files except pre-existing Navbar.tsx:22 set-state-in-effect. Commit: `e8c27a1`.
