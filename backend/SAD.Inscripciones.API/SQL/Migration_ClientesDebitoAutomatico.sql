-- Migration: campos de débito automático en Clientes.
-- Se replican (a futuro) en Tango: CA_1096_DEBITO_AUTOMATICO, CA_1096_TARJETA,
-- CA_1096_VENC_TARJETA, CA_1096_NRO_TARJETA. Aquí guardamos el número de tarjeta
-- cifrado con AES-GCM (CryptoService usa Email:EncryptionKey).
ALTER TABLE Clientes
    ADD COLUMN DebitoAutomatico TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN MarcaTarjeta VARCHAR(20) NULL,
    ADD COLUMN NumeroTarjetaCifrado TEXT NULL,
    ADD COLUMN TarjetaUltimos4 VARCHAR(4) NULL,
    ADD COLUMN VencimientoTarjeta VARCHAR(5) NULL,
    ADD COLUMN FechaAltaDebito DATETIME NULL;
