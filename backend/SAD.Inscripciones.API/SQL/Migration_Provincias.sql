-- Migration: Tabla Provincias
CREATE TABLE IF NOT EXISTS Provincias (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(10) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    UNIQUE KEY UQ_Provincias_Codigo (Codigo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
