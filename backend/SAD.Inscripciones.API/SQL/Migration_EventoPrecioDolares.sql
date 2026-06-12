-- Migration: EventoPrecioDolares
-- Precio en dólares (USD) para las categorías extranjeras, cobrado vía PayPal.
ALTER TABLE EventoPrecios ADD COLUMN PrecioDolares DECIMAL(18,2) NULL;
