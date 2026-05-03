-- Migration: Tabla Vendedores
-- Mirror local de GVA23 + cuentas de tesoreria definidas en CAMPOS_ADICIONALES
-- (CTA_CAJA, CTA_TRANSFERENCIA). Usado para resolver la cuenta haber cuando un
-- capitulo cobra presencialmente.
CREATE TABLE IF NOT EXISTS Vendedores (
    CodVended VARCHAR(20) NOT NULL PRIMARY KEY,
    CtaCaja INT NOT NULL DEFAULT 0,
    CtaTransferencia INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
