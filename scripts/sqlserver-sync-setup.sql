-- ============================================================
-- SAD Inscripciones - Sync Setup para SQL Server (Tango)
-- Crear tabla SyncQueue y triggers en las tablas origen
-- Ejecutar en la base de datos de Tango
-- ============================================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SyncQueue' AND xtype='U')
CREATE TABLE SyncQueue (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Tabla VARCHAR(50) NOT NULL,
    Operacion VARCHAR(10) NOT NULL,  -- INSERT, UPDATE, DELETE
    ClaveValor VARCHAR(100) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    Procesado BIT NOT NULL DEFAULT 0
);
GO

CREATE INDEX IX_SyncQueue_Pendientes ON SyncQueue(Procesado, Id) WHERE Procesado = 0;
GO

-- ============================================================
-- Trigger: GVA14 (Clientes) → SyncQueue
-- ============================================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_GVA14_Sync')
    DROP TRIGGER trg_GVA14_Sync;
GO

CREATE TRIGGER trg_GVA14_Sync ON GVA14
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT / UPDATE
    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Clientes',
           CASE WHEN EXISTS (SELECT 1 FROM deleted d WHERE d.CUIT = i.CUIT) THEN 'UPDATE' ELSE 'INSERT' END,
           i.CUIT
    FROM inserted i;

    -- DELETE
    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Clientes', 'DELETE', d.CUIT
    FROM deleted d
    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.CUIT = d.CUIT);
END;
GO

-- ============================================================
-- Trigger: STA11 (Articulos) → SyncQueue
-- ============================================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_STA11_Sync')
    DROP TRIGGER trg_STA11_Sync;
GO

CREATE TRIGGER trg_STA11_Sync ON STA11
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Articulos',
           CASE WHEN EXISTS (SELECT 1 FROM deleted d WHERE d.COD_ARTICU = i.COD_ARTICU) THEN 'UPDATE' ELSE 'INSERT' END,
           i.COD_ARTICU
    FROM inserted i;

    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Articulos', 'DELETE', d.COD_ARTICU
    FROM deleted d
    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.COD_ARTICU = d.COD_ARTICU);
END;
GO

-- ============================================================
-- Trigger: GVA18 (Provincias) → SyncQueue
-- ============================================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_GVA18_Sync')
    DROP TRIGGER trg_GVA18_Sync;
GO

CREATE TRIGGER trg_GVA18_Sync ON GVA18
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Provincias',
           CASE WHEN EXISTS (SELECT 1 FROM deleted d WHERE d.COD_PROVIN = i.COD_PROVIN) THEN 'UPDATE' ELSE 'INSERT' END,
           i.COD_PROVIN
    FROM inserted i;

    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'Provincias', 'DELETE', d.COD_PROVIN
    FROM deleted d
    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.COD_PROVIN = d.COD_PROVIN);
END;
GO

-- ============================================================
-- Trigger: GVA07 (Imputaciones) → SyncQueue
-- ============================================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_GVA07_Sync')
    DROP TRIGGER trg_GVA07_Sync;
GO

CREATE TRIGGER trg_GVA07_Sync ON GVA07
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT / UPDATE
    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'CuentaCorriente',
           CASE WHEN EXISTS (SELECT 1 FROM deleted d WHERE d.T_COMP = i.T_COMP AND d.N_COMP = i.N_COMP) THEN 'UPDATE' ELSE 'INSERT' END,
           i.T_COMP + '|' + i.N_COMP
    FROM inserted i;

    -- DELETE
    INSERT INTO SyncQueue (Tabla, Operacion, ClaveValor)
    SELECT 'CuentaCorriente', 'UPDATE', d.T_COMP + '|' + d.N_COMP
    FROM deleted d
    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.T_COMP = d.T_COMP AND i.N_COMP = d.N_COMP);
END;
GO

PRINT 'SyncQueue table and triggers created successfully.';
GO
