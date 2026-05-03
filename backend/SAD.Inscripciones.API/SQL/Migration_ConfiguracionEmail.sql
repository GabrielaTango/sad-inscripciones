-- Migration: ConfiguracionEmail
-- Single-row config para envío de mails transaccionales (SMTP).
-- El password queda cifrado AES-GCM con la clave Email:EncryptionKey de appsettings.
-- Asunto y BccCopia son opcionales; el cuerpo viene del template en disco.
CREATE TABLE IF NOT EXISTS ConfiguracionEmail (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Host VARCHAR(255) NOT NULL DEFAULT '',
    Port INT NOT NULL DEFAULT 587,
    EnableSsl TINYINT(1) NOT NULL DEFAULT 1,
    Usuario VARCHAR(255) NOT NULL DEFAULT '',
    PasswordCifrado TEXT NOT NULL,
    FromEmail VARCHAR(255) NOT NULL DEFAULT '',
    FromName VARCHAR(255) NOT NULL DEFAULT 'Inscripciones SAD',
    ReplyTo VARCHAR(255) NULL,
    BccCopia VARCHAR(500) NULL,
    Asunto VARCHAR(500) NOT NULL DEFAULT 'Confirmación de inscripción - {{Evento}}',
    Activo TINYINT(1) NOT NULL DEFAULT 0,
    IgnorarCertificadoSsl TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sembrar la única fila si no existe (Id=1 fijo).
INSERT INTO ConfiguracionEmail (Id, Host, Port, EnableSsl, Usuario, PasswordCifrado, FromEmail, FromName, Asunto, Activo)
SELECT 1, '', 587, 1, '', '', '', 'Inscripciones SAD', 'Confirmación de inscripción - {{Evento}}', 0
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionEmail WHERE Id = 1);

-- Idempotente: agrega la columna si la tabla ya existía sin ella.
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ConfiguracionEmail'
      AND COLUMN_NAME = 'IgnorarCertificadoSsl'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE ConfiguracionEmail ADD COLUMN IgnorarCertificadoSsl TINYINT(1) NOT NULL DEFAULT 0 AFTER Activo',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
