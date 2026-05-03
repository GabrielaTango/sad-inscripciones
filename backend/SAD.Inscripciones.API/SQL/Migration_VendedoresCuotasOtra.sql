-- Migration: Cuentas extra por vendedor (capítulo)
-- CtaCuotas: cuenta haber al cobrar cuotas/comprobantes específicos del socio.
-- CtaOtra:   cuenta haber al cobrar a cuenta (sin imputar comprobantes).
-- Las viejas (CtaCaja / CtaTransferencia) quedan, no se usan para resolver la
-- cuenta haber del recibo pero se mantienen sincronizadas desde GVA23 por si
-- algún reporte las necesita.
ALTER TABLE Vendedores
    ADD COLUMN CtaCuotas INT NOT NULL DEFAULT 0,
    ADD COLUMN CtaOtra   INT NOT NULL DEFAULT 0;
