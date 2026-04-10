-- Migration: Tabla Clientes
CREATE TABLE IF NOT EXISTS Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    RazonSoci VARCHAR(200) NOT NULL,
    Domicilio VARCHAR(200) NULL,
    CodPostal VARCHAR(20) NULL,
    CodProvin VARCHAR(10) NULL,
    UNIQUE KEY UQ_Clientes_Cuit (Cuit)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
