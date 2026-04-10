-- Migration: Agregar columna TerminosArchivo a Eventos
ALTER TABLE Eventos ADD TerminosArchivo NVARCHAR(500) NULL;
