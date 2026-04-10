CREATE TABLE IF NOT EXISTS PagosCuentaCorriente (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    Comprobantes TEXT NOT NULL,              -- JSON array de comprobantes seleccionados
    ExternalReference VARCHAR(100) NOT NULL,
    PreferenceId VARCHAR(100) NULL,
    EstadoPago VARCHAR(20) NOT NULL DEFAULT 'Pendiente',  -- Pendiente, Aprobado, Rechazado
    MpPaymentId BIGINT NULL,
    FechaPago DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX IX_PagoCC_Cuit (Cuit),
    INDEX IX_PagoCC_ExtRef (ExternalReference)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
