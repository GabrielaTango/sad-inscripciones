-- Migration: Tabla ResumenCuenta (saldos pendientes sincronizados desde Tango)
CREATE TABLE IF NOT EXISTS ResumenCuenta (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    TComp VARCHAR(10) NOT NULL,
    NComp VARCHAR(20) NOT NULL,
    FechaVto DATE NOT NULL,
    Saldo DECIMAL(18,2) NOT NULL,
    UNIQUE KEY UQ_ResumenCuenta (Cuit, TComp, NComp, FechaVto),
    INDEX IX_ResumenCuenta_Cuit (Cuit)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
