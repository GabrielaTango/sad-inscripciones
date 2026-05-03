ALTER TABLE Pagos
    ADD COLUMN SincronizadoTango BOOLEAN NOT NULL DEFAULT 0;

CREATE INDEX IX_Pagos_SincronizadoTango
    ON Pagos (EstadoPago, SincronizadoTango, DeletedAt);
