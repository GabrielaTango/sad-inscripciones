-- Migration: Tabla Articulos
CREATE TABLE IF NOT EXISTS Articulos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CodArticu VARCHAR(50) NOT NULL,
    Descripcio VARCHAR(200) NOT NULL,
    UNIQUE KEY UQ_Articulos_CodArticu (CodArticu)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
