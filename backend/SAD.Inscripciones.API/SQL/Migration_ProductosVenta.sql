-- Migration: ProductosVenta
-- Standalone product-sale module (e.g. merchandise), fully separate from the
-- eventos/inscripciones circuit and never synced to Tango.
-- - Productos: catalog managed from the admin. CamposExtra is a JSON array
--   describing the dynamic extra fields shown on the public purchase form
--   (schema: [{ "key", "label", "type": "text|number|select", "options": [], "required" }]).
-- - VentasProducto: one row per purchase attempt. DatosExtra holds the buyer's
--   answers to CamposExtra as JSON. A sale only counts once Estado = 'Pagada',
--   set idempotently by the MercadoPago webhook via MpPaymentId (UNIQUE).
CREATE TABLE IF NOT EXISTS Productos (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(200) NOT NULL,
    Descripcion TEXT NULL,
    Precio DECIMAL(12,2) NOT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    ImagenUrl VARCHAR(500) NULL,
    CamposExtra MEDIUMTEXT NULL,             -- JSON array de campos dinámicos del formulario de compra
    MailAsunto VARCHAR(500) NULL,
    MailCuerpoHtml MEDIUMTEXT NULL,
    FechaAlta DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS VentasProducto (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductoId INT NOT NULL,
    PublicRef VARCHAR(50) NOT NULL,
    Dni VARCHAR(20) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    Apellido VARCHAR(200) NOT NULL,
    Email VARCHAR(200) NOT NULL,
    DatosExtra MEDIUMTEXT NULL,              -- JSON con las respuestas del comprador a Productos.CamposExtra
    Importe DECIMAL(12,2) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',  -- Pendiente, Pagada, Rechazada
    MpPaymentId BIGINT NULL,
    FechaAlta DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaPago DATETIME NULL,
    MailEnviado TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT FK_VentasProducto_Producto FOREIGN KEY (ProductoId) REFERENCES Productos (Id),
    CONSTRAINT UQ_VentasProducto_MpPaymentId UNIQUE (MpPaymentId),
    CONSTRAINT UQ_VentasProducto_PublicRef UNIQUE (PublicRef),
    INDEX IX_VentasProducto_Producto_Estado (ProductoId, Estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
