-- Migration: Sistema de Promociones con Cupones
-- Fecha: 2026-04-02

-- Tabla de configuración de promociones
CREATE TABLE Promociones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    TipoAlumnoId INT NULL,
    CantidadCursosRequeridos INT NOT NULL DEFAULT 1,
    PeriodoMeses INT NOT NULL DEFAULT 12,
    TipoDescuento NVARCHAR(20) NOT NULL,       -- 'Porcentaje' o 'MontoFijo'
    Valor DECIMAL(18,2) NOT NULL,
    Acumulable BIT NOT NULL DEFAULT 0,
    FechaVigenciaDesde DATETIME2 NOT NULL,
    FechaVigenciaHasta DATETIME2 NOT NULL,
    DiasValidezCupon INT NOT NULL DEFAULT 90,
    Activo BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_Promociones_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
);

-- Tabla de cupones generados
CREATE TABLE PromocionCupones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PromocionId INT NOT NULL,
    Documento NVARCHAR(50) NOT NULL,
    Codigo NVARCHAR(10) NOT NULL,
    TipoDescuento NVARCHAR(20) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    Acumulable BIT NOT NULL DEFAULT 0,
    Usado BIT NOT NULL DEFAULT 0,
    FechaUso DATETIME2 NULL,
    InscripcionDestinoId INT NULL,
    FechaVencimiento DATETIME2 NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_PromocionCupones_Promociones FOREIGN KEY (PromocionId) REFERENCES Promociones(Id),
    CONSTRAINT FK_PromocionCupones_InscripcionDestino FOREIGN KEY (InscripcionDestinoId) REFERENCES Inscripciones(Id)
);

CREATE UNIQUE INDEX IX_PromocionCupones_Codigo ON PromocionCupones(Codigo) WHERE DeletedAt IS NULL;

-- Tabla pivote: inscripciones consumidas para generar cada cupón
CREATE TABLE PromocionCuponInscripciones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PromocionCuponId INT NOT NULL,
    InscripcionId INT NOT NULL,
    CONSTRAINT FK_PCI_PromocionCupones FOREIGN KEY (PromocionCuponId) REFERENCES PromocionCupones(Id),
    CONSTRAINT FK_PCI_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id)
);

CREATE INDEX IX_PCI_PromocionCuponId ON PromocionCuponInscripciones(PromocionCuponId);
CREATE INDEX IX_PCI_InscripcionId ON PromocionCuponInscripciones(InscripcionId);
