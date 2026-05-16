-- Migration: sumar CodClient (GVA14.COD_CLIENT de Tango) a la tabla Clientes.
-- Usado por el frontend para mostrar el identificador de pago en Pago Mis Cuentas
-- (cod_client padleft a 8 dígitos).
ALTER TABLE Clientes
    ADD COLUMN CodClient VARCHAR(20) NULL;
