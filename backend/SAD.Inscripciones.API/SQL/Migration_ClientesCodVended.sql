-- Migration: CodVended en Clientes
-- Mirror del COD_VENDED de GVA14. Se usa para que un capitulo solo vea/cobre
-- a sus propios socios.
ALTER TABLE Clientes
    ADD COLUMN CodVended VARCHAR(20) NULL;

CREATE INDEX IX_Clientes_CodVended ON Clientes (CodVended);
