-- Migration: Soporte de Capitulo (vendedor) en Usuarios
-- EsCapitulo = 1 habilita login por capitulo y agrega claim codVended al JWT.
-- CodVended FK a Vendedores (sincronizado desde GVA23).
ALTER TABLE Usuarios
    ADD COLUMN CodVended VARCHAR(20) NULL,
    ADD COLUMN EsCapitulo TINYINT(1) NOT NULL DEFAULT 0,
    ADD CONSTRAINT FK_Usuarios_Vendedores
        FOREIGN KEY (CodVended) REFERENCES Vendedores(CodVended)
        ON UPDATE CASCADE ON DELETE RESTRICT;

CREATE INDEX IX_Usuarios_CodVended ON Usuarios (CodVended);
