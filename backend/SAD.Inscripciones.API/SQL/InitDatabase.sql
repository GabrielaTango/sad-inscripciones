-- Script de inicialización de base de datos SAD Inscripciones
-- La tabla GVA14 ya existe en la base de datos (Tango Gestión) y NO se crea aquí.

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Inscripciones' AND xtype='U')
BEGIN
    CREATE TABLE Inscripciones (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Apellido NVARCHAR(100) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Telefono NVARCHAR(50) NULL,
        Especialidad NVARCHAR(100) NOT NULL,
        Matricula NVARCHAR(50) NOT NULL,
        Provincia NVARCHAR(100) NOT NULL,
        TipoSocio NVARCHAR(50) NOT NULL,
        Mensaje NVARCHAR(MAX) NULL,
        FechaInscripcion DATETIME NOT NULL DEFAULT GETUTCDATE(),
        Estado NVARCHAR(50) NOT NULL DEFAULT 'Pendiente'
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Contactos' AND xtype='U')
BEGIN
    CREATE TABLE Contactos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Asunto NVARCHAR(200) NOT NULL,
        Mensaje NVARCHAR(MAX) NOT NULL,
        FechaEnvio DATETIME NOT NULL DEFAULT GETUTCDATE(),
        Leido BIT NOT NULL DEFAULT 0
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Eventos' AND xtype='U')
BEGIN
    CREATE TABLE Eventos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Tipo NVARCHAR(100) NOT NULL,
        Titulo NVARCHAR(200) NOT NULL,
        Fecha DATETIME NOT NULL,
        Lugar NVARCHAR(200) NOT NULL,
        Modalidad NVARCHAR(50) NOT NULL,
        Descripcion NVARCHAR(MAX) NULL,
        Activo BIT NOT NULL DEFAULT 1
    );
END
GO
