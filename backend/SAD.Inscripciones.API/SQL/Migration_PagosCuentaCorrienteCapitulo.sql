-- Migration: Cobros presenciales por Capitulo sobre PagosCuentaCorriente
--
-- Reusa la tabla existente para no duplicar el pipeline de sync a Tango.
-- Diferencias respecto al pago por MercadoPago del socio:
--   * CodVended: capitulo que tomo el cobro (NULL si lo pago el socio por MP).
--   * MedioPago: 'Contado' | 'Transferencia' (NULL para cobros MP del socio).
--   * CtaTesoreria: snapshot de la cuenta haber resuelta del vendedor al cobrar.
--     Snapshot para que un cambio posterior en la config del vendedor en Tango
--     no altere el asiento ya generado.
--   * Comprobantes pasa a NULL-able: NULL/'[]' significa pago a cuenta (saldo
--     a favor en Tango, sin imputaciones GVA07).
ALTER TABLE PagosCuentaCorriente
    MODIFY COLUMN Comprobantes TEXT NULL,
    ADD COLUMN CodVended VARCHAR(20) NULL,
    ADD COLUMN MedioPago VARCHAR(20) NULL,
    ADD COLUMN CtaTesoreria INT NULL;

CREATE INDEX IX_PagoCC_CodVended ON PagosCuentaCorriente (CodVended);
