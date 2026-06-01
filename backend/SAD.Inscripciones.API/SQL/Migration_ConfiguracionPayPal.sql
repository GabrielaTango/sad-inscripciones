-- Migration: ConfiguracionPayPal
-- Single-row config para PayPal. El Client-ID es público (se expone al frontend para
-- cargar el SDK de PayPal), por eso NO se cifra. La moneda es fija USD por defecto.
CREATE TABLE IF NOT EXISTS ConfiguracionPayPal (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ClientId VARCHAR(255) NOT NULL DEFAULT '',
    Moneda VARCHAR(10) NOT NULL DEFAULT 'USD',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sembrar la única fila si no existe (Id=1 fijo).
INSERT INTO ConfiguracionPayPal (Id, ClientId, Moneda)
SELECT 1, '', 'USD'
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionPayPal WHERE Id = 1);
