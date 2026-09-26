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
- Branch: `feat/productos-venta`. Strategy: ask-on-risk. Forecast likely > 400 authored lines -> chain strategy to be asked before exceeding.

## Tasks
- [x] T1 SQL migration `Migration_ProductosVenta.sql`, models, repositories (Productos, VentasProducto) + DI registration.
- [ ] T2 Products CRUD: service, DTOs, `ProductosController` (public GET active, Admin POST/PUT/DELETE); admin pages list + form with dynamic-field and email editor; sidebar entry.
- [ ] T3 Pending sale + MP preference (`CrearPreferenciaVentaAsync`) + `POST /api/ventas-producto`; public purchase page `/productos/:id/comprar` rendering dynamic fields.
- [ ] T4 `VentaProductoWebhookController` with idempotent confirmation (row lock, amount check, UNIQUE MpPaymentId) + public result page `/productos/pago/resultado`.
- [ ] T5 Confirmation email `EnviarConfirmacionVentaAsync` triggered only on the Pendiente -> Pagada transition.
- [ ] T6 Admin paid-sales list with product filter, extra-field columns and export.

## Progress
- Branch created from `main`.
- T1 done: `SQL/Migration_ProductosVenta.sql`, `Models/{Producto,VentaProducto,CampoExtraProducto}.cs`, `Repositories/{ProductoRepository,VentaProductoRepository}.cs` + interfaces, DI registered in `Program.cs`. `dotnet build` succeeds (0 errors). Commit: TBD.

## Next step
T2.
