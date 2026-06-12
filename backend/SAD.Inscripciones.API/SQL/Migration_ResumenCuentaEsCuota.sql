-- Migration: marcar cada comprobante de la cuenta corriente como cuota societaria u "otro".
-- Antes el sync traía SOLO comprobantes con artículo "CUOTA"; ahora trae todos y
-- guarda EsCuota=1 cuando algún artículo del comprobante es cuota societaria, 0 si no.
-- Los registros previos quedan en 1 (comportamiento histórico: todo era cuota).
ALTER TABLE ResumenCuenta
    ADD COLUMN EsCuota TINYINT(1) NOT NULL DEFAULT 1;
