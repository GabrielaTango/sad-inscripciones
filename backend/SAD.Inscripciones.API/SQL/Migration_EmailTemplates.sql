-- Migration: EmailTemplates
-- Templates HTML editables desde el admin (editor visual Unlayer).
-- - Codigo es el identificador lógico que usa el código (ej "inscripcion-confirmada").
-- - BodyHtml es el HTML inline-styled que se envía.
-- - BodyJson es el design state del editor Unlayer (para reabrir y seguir editando).
--   Si BodyJson es NULL = el template fue cargado desde el archivo seed y nunca se editó visualmente.
CREATE TABLE IF NOT EXISTS EmailTemplates (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(80) NOT NULL UNIQUE,
    Nombre VARCHAR(200) NOT NULL,
    Asunto VARCHAR(500) NOT NULL DEFAULT '',
    BodyHtml MEDIUMTEXT NOT NULL,
    BodyJson MEDIUMTEXT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed mínimo de la única fila esperada por v1. El BodyHtml real lo carga el backend
-- desde EmailTemplates/inscripcion-confirmada.html la primera vez que se accede,
-- así no duplicamos el HTML acá.
INSERT INTO EmailTemplates (Codigo, Nombre, Asunto, BodyHtml, BodyJson, Activo)
SELECT 'inscripcion-confirmada',
       'Confirmación de inscripción',
       'Confirmación de inscripción - {{Evento}}',
       '',
       NULL,
       1
WHERE NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Codigo = 'inscripcion-confirmada');
