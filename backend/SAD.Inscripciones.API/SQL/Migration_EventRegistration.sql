-- ============================================================
-- SAD Inscripciones - Event Registration System Migration
-- Creates all tables for the complete event/inscription model
-- ============================================================

-- 1. TiposEvento (no FK)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TiposEvento' AND xtype='U')
CREATE TABLE TiposEvento (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL
);

-- 2. TiposAlumno (no FK)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TiposAlumno' AND xtype='U')
CREATE TABLE TiposAlumno (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL
);

-- 3. Eventos (FK -> TiposEvento)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Eventos' AND xtype='U')
BEGIN
    DROP TABLE IF EXISTS Eventos;
END;

-- Recreate Eventos with new schema
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Eventos' AND COLUMN_NAME = 'TipoEventoId')
BEGIN
    -- If old Eventos table exists, drop it
    IF EXISTS (SELECT * FROM sysobjects WHERE name='Eventos' AND xtype='U')
        DROP TABLE Eventos;

    CREATE TABLE Eventos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TipoEventoId INT NOT NULL,
        Titulo NVARCHAR(200) NOT NULL,
        Descripcion NVARCHAR(MAX) NULL,
        FechaInicio DATETIME NOT NULL,
        FechaFin DATETIME NOT NULL,
        FechaCierreInscripcion DATETIME NOT NULL,
        Lugar NVARCHAR(300) NULL,
        Modalidad NVARCHAR(50) NOT NULL,
        MaxInscriptos INT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NULL,
        UpdatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
        DeletedAt DATETIME NULL,
        CONSTRAINT FK_Eventos_TiposEvento FOREIGN KEY (TipoEventoId) REFERENCES TiposEvento(Id)
    );
END;

-- 4. EventoPrecios (FK -> Eventos, TiposAlumno)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventoPrecios' AND xtype='U')
CREATE TABLE EventoPrecios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    ArticuloCodigo NVARCHAR(50) NULL,
    PrecioBase DECIMAL(18,2) NOT NULL,
    PermiteDescuento BIT NOT NULL DEFAULT 1,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoPrecios_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_EventoPrecios_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
);

-- 5. EventoProvinciaBeneficios (FK -> Eventos)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventoProvinciaBeneficios' AND xtype='U')
CREATE TABLE EventoProvinciaBeneficios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    ProvinciaCodigo NVARCHAR(50) NOT NULL,
    AplicaPrecioSocio BIT NOT NULL DEFAULT 0,
    PorcentajeDescuento DECIMAL(5,2) NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoProvinciaBeneficios_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id)
);

-- 6. EventoPromociones (FK -> Eventos)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventoPromociones' AND xtype='U')
CREATE TABLE EventoPromociones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    CantidadCursosRequeridos INT NOT NULL DEFAULT 1,
    PeriodoMeses INT NOT NULL DEFAULT 12,
    PorcentajeDescuento DECIMAL(5,2) NOT NULL DEFAULT 0,
    Acumulable BIT NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoPromociones_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id)
);

-- 7. EventoArticuloRegalos (FK -> Eventos, TiposAlumno)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventoArticuloRegalos' AND xtype='U')
CREATE TABLE EventoArticuloRegalos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    ArticuloCodigo NVARCHAR(50) NOT NULL,
    DescripcionArticulo NVARCHAR(200) NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    CondicionEspecial NVARCHAR(500) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoArticuloRegalos_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_EventoArticuloRegalos_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
);

-- 8. Inscripciones (FK -> Eventos, TiposAlumno)
IF EXISTS (SELECT * FROM sysobjects WHERE name='Inscripciones' AND xtype='U')
BEGIN
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Inscripciones' AND COLUMN_NAME = 'Nombre')
        DROP TABLE Inscripciones;
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Inscripciones' AND xtype='U')
CREATE TABLE Inscripciones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200) NOT NULL,
    Telefono NVARCHAR(50) NULL,
    Documento NVARCHAR(20) NULL,
    Provincia NVARCHAR(50) NULL,
    PrecioBase DECIMAL(18,2) NOT NULL DEFAULT 0,
    DescuentoAplicado DECIMAL(18,2) NOT NULL DEFAULT 0,
    PrecioFinal DECIMAL(18,2) NOT NULL DEFAULT 0,
    Estado NVARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    Observaciones NVARCHAR(MAX) NULL,
    FechaInscripcion DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Inscripciones_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_Inscripciones_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
);

-- 9. Pagos (FK -> Inscripciones)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Pagos' AND xtype='U')
CREATE TABLE Pagos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InscripcionId INT NOT NULL,
    MedioPago NVARCHAR(50) NOT NULL,
    EstadoPago NVARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    Monto DECIMAL(18,2) NOT NULL,
    ReferenciaExterna NVARCHAR(200) NULL,
    FechaPago DATETIME NULL,
    Observaciones NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Pagos_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id)
);

-- 10. BecaEventos (FK -> Eventos)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BecaEventos' AND xtype='U')
CREATE TABLE BecaEventos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventoId INT NOT NULL,
    NombreCampana NVARCHAR(200) NOT NULL,
    TipoDescuento NVARCHAR(50) NOT NULL, -- 'Porcentaje' o 'MontoFijo'
    Valor DECIMAL(18,2) NOT NULL,
    CantidadTotalCodigos INT NOT NULL DEFAULT 1,
    FechaVencimiento DATETIME NULL,
    Acumulable BIT NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_BecaEventos_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id)
);

-- 11. BecaCodigos (FK -> BecaEventos, Inscripciones)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BecaCodigos' AND xtype='U')
CREATE TABLE BecaCodigos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BecaEventoId INT NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Usado BIT NOT NULL DEFAULT 0,
    FechaUso DATETIME NULL,
    InscripcionId INT NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_BecaCodigos_BecaEventos FOREIGN KEY (BecaEventoId) REFERENCES BecaEventos(Id),
    CONSTRAINT FK_BecaCodigos_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
    CONSTRAINT UQ_BecaCodigos_Codigo UNIQUE (Codigo)
);
