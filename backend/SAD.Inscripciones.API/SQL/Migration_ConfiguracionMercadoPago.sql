-- Migration: ConfiguracionMercadoPago
-- Single-row config para credenciales de MercadoPago.
-- El AccessToken queda cifrado AES-GCM con la clave Email:EncryptionKey de appsettings
-- (misma clave que la config de email; CryptoService es compartido).
CREATE TABLE IF NOT EXISTS ConfiguracionMercadoPago (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    AccessTokenCifrado TEXT NOT NULL,
    FrontendBaseUrl VARCHAR(500) NOT NULL DEFAULT '',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sembrar la única fila si no existe (Id=1 fijo).
INSERT INTO ConfiguracionMercadoPago (Id, AccessTokenCifrado, FrontendBaseUrl)
SELECT 1, '', ''
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionMercadoPago WHERE Id = 1);
