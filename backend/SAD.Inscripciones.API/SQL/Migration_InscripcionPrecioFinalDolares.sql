-- Migration: InscripcionPrecioFinalDolares
-- Monto final en USD de la inscripción (PrecioDolares con el descuento de beca de tipo
-- porcentaje ya aplicado). Se persiste solo cuando hubo descuento de beca; para el resto
-- queda NULL y el cobro usa el PrecioDolares vigente de la categoría.
ALTER TABLE Inscripciones ADD COLUMN PrecioFinalDolares DECIMAL(18,2) NULL;
