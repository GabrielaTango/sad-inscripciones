-- Migration: ConfiguracionContacto
-- Single-row config con el email destino al que se mandan las consultas
-- enviadas desde el formulario público de /contacto.
CREATE TABLE IF NOT EXISTS ConfiguracionContacto (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    EmailDestino VARCHAR(255) NOT NULL DEFAULT '',
    Activo TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sembrar la única fila si no existe (Id=1 fijo).
INSERT INTO ConfiguracionContacto (Id, EmailDestino, Activo)
SELECT 1, '', 0
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionContacto WHERE Id = 1);
