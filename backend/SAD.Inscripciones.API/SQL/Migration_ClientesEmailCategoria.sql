-- Migration: Email y Categoria en Clientes
-- Mirror de GVA14.E_MAIL (email del cliente) y de GVA05.NOMBRE_ZON (categoria de
-- socio, obtenida por LEFT JOIN GVA14.COD_ZONA = GVA05.COD_ZONA). Se usan para
-- mostrar estos datos en el listado de socios del capitulo.
ALTER TABLE Clientes
    ADD COLUMN Email VARCHAR(200) NULL,
    ADD COLUMN Categoria VARCHAR(100) NULL;
