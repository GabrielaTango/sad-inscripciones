-- Migration: TipoAlumnoExtranjero
-- Marca un tipo de alumno como extranjero. Las categorías extranjeras pagan en
-- dólares vía PayPal (en vez de MercadoPago).
ALTER TABLE TiposAlumno ADD COLUMN Extranjero TINYINT(1) NOT NULL DEFAULT 0;
