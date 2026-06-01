-- Migration: PagosMoneda
-- Distingue la moneda del pago. Los pagos por MercadoPago quedan en ARS (default);
-- los de PayPal se registran en USD. Tango (que opera en ARS) ignora por ahora los USD.
ALTER TABLE Pagos ADD COLUMN Moneda VARCHAR(10) NOT NULL DEFAULT 'ARS';
