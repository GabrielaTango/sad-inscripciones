-- Migration: Tabla Usuarios (administradores internos)
-- Independiente de la tabla GVA14 de socios

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(200) NOT NULL,
        NombreCompleto NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NULL,
        Activo BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NULL,
        UpdatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        DeletedAt DATETIME2 NULL,

        CONSTRAINT UQ_Usuarios_Username UNIQUE (Username)
    );

    CREATE INDEX IX_Usuarios_Username ON Usuarios (Username) WHERE DeletedAt IS NULL;
END
GO
