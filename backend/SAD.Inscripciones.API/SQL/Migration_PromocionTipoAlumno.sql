-- Add TipoAlumnoId to EventoPromociones (nullable, NULL = applies to all student types)
ALTER TABLE EventoPromociones
ADD TipoAlumnoId INT NULL;

ALTER TABLE EventoPromociones
ADD CONSTRAINT FK_EventoPromociones_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id);
