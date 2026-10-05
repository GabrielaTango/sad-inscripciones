-- Migration: ProductosImagen
-- Product images are uploaded by the admin and stored in MySQL as binary.
-- - ProductoImagenes: one row per product, kept in its own table so product
--   listings never load the blob. Deleted automatically with its product.
--   UpdatedAt is used as the cache-busting version of the image URL.
-- - Productos.ImagenUrl (hand-typed URL) is dropped; the API now computes the
--   image URL from the existence of a ProductoImagenes row.
CREATE TABLE IF NOT EXISTS ProductoImagenes (
    ProductoId INT NOT NULL PRIMARY KEY,
    Contenido MEDIUMBLOB NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT FK_ProductoImagenes_Producto FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE Productos DROP COLUMN ImagenUrl;
