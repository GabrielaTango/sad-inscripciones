-- Add installment pricing to EventoPrecios
ALTER TABLE EventoPrecios ADD PrecioCuotas DECIMAL(18,2) NULL;
ALTER TABLE EventoPrecios ADD CantidadCuotas INT NOT NULL DEFAULT 6;

-- Add installment pricing to Inscripciones
ALTER TABLE Inscripciones ADD PrecioFinalCuotas DECIMAL(18,2) NULL;
ALTER TABLE Inscripciones ADD CantidadCuotas INT NULL;
