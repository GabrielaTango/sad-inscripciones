-- Migration: campos para cupones Pago Fácil sobre PagosCuentaCorriente
--
-- CodigoBarra: 42 dígitos (Interleaved 2 of 5) calculados al generar el cupón.
--   Persistimos el código para que el mismo cupón se vea idéntico cuando el socio
--   lo reimprime y para que el admin pueda buscarlo en logs/reportes.
-- FechaVencimiento: a los 30 días de la generación; lo usa el front para mostrar
--   "válido hasta" y la query de cleanup para no borrar antes de tiempo.
ALTER TABLE PagosCuentaCorriente
    ADD COLUMN CodigoBarra VARCHAR(50) NULL,
    ADD COLUMN FechaVencimiento DATETIME NULL;
