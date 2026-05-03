-- Agrega soporte de sincronización a Tango para PagosCuentaCorriente.
-- SincronizadoTango: 1 cuando el recibo fue cargado en Tango por el SyncService.
-- MontoImputado: monto efectivamente imputado contra comprobantes (puede ser < Monto si algún comprobante ya tenía saldo menor en Tango).

ALTER TABLE PagosCuentaCorriente
    ADD COLUMN SincronizadoTango TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN MontoImputado DECIMAL(18,2) NULL,
    ADD INDEX IX_PagoCC_SincTango (EstadoPago, SincronizadoTango);
