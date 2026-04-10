-- ============================================================
-- SAD Inscripciones - MySQL Full Database Init
-- ============================================================

USE SADInscripciones;

-- 1. TiposEvento
CREATE TABLE IF NOT EXISTS TiposEvento (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. TiposAlumno
CREATE TABLE IF NOT EXISTS TiposAlumno (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Eventos
CREATE TABLE IF NOT EXISTS Eventos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TipoEventoId INT NOT NULL,
    Titulo VARCHAR(200) NOT NULL,
    Descripcion TEXT NULL,
    FechaInicio DATETIME NOT NULL,
    FechaFin DATETIME NOT NULL,
    FechaCierreInscripcion DATETIME NOT NULL,
    Lugar VARCHAR(300) NULL,
    Modalidad VARCHAR(50) NOT NULL,
    MaxInscriptos INT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    SoloSocios TINYINT(1) NOT NULL DEFAULT 0,
    TerminosArchivo VARCHAR(500) NULL,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Eventos_TiposEvento FOREIGN KEY (TipoEventoId) REFERENCES TiposEvento(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. EventoPrecios
CREATE TABLE IF NOT EXISTS EventoPrecios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    ArticuloCodigo VARCHAR(50) NULL,
    PrecioBase DECIMAL(18,2) NOT NULL,
    PrecioCuotas DECIMAL(18,2) NULL,
    CantidadCuotas INT NOT NULL DEFAULT 6,
    PermiteDescuento TINYINT(1) NOT NULL DEFAULT 1,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoPrecios_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_EventoPrecios_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. EventoProvinciaBeneficios
CREATE TABLE IF NOT EXISTS EventoProvinciaBeneficios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventoId INT NOT NULL,
    ProvinciaCodigo VARCHAR(50) NOT NULL,
    AplicaPrecioSocio TINYINT(1) NOT NULL DEFAULT 0,
    PorcentajeDescuento DECIMAL(5,2) NOT NULL DEFAULT 0,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoProvBen_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. EventoArticuloRegalos
CREATE TABLE IF NOT EXISTS EventoArticuloRegalos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    ArticuloCodigo VARCHAR(50) NOT NULL,
    DescripcionArticulo VARCHAR(200) NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    CondicionEspecial VARCHAR(500) NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_EventoArtReg_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_EventoArtReg_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 7. Inscripciones
CREATE TABLE IF NOT EXISTS Inscripciones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventoId INT NOT NULL,
    TipoAlumnoId INT NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Apellido VARCHAR(100) NOT NULL,
    Email VARCHAR(200) NOT NULL,
    Telefono VARCHAR(50) NULL,
    Documento VARCHAR(20) NULL,
    Provincia VARCHAR(50) NULL,
    PrecioBase DECIMAL(18,2) NOT NULL DEFAULT 0,
    DescuentoAplicado DECIMAL(18,2) NOT NULL DEFAULT 0,
    PrecioFinal DECIMAL(18,2) NOT NULL DEFAULT 0,
    PrecioFinalCuotas DECIMAL(18,2) NULL,
    CantidadCuotas INT NULL,
    MontoReserva DECIMAL(18,2) NULL,
    Estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    Observaciones TEXT NULL,
    FechaInscripcion DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    FechaNacimiento DATE NULL,
    Domicilio VARCHAR(200) NULL,
    CodigoPostal VARCHAR(20) NULL,
    Localidad VARCHAR(100) NULL,
    Pais VARCHAR(100) NULL,
    Celular VARCHAR(50) NULL,
    Profesion VARCHAR(100) NULL,
    Especialidad VARCHAR(100) NULL,
    Institucion VARCHAR(200) NULL,
    Sector VARCHAR(200) NULL,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Inscripciones_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_Inscripciones_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 8. Pagos
CREATE TABLE IF NOT EXISTS Pagos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    InscripcionId INT NOT NULL,
    MedioPago VARCHAR(50) NOT NULL,
    EstadoPago VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    Monto DECIMAL(18,2) NOT NULL,
    ReferenciaExterna VARCHAR(200) NULL,
    FechaPago DATETIME NULL,
    Observaciones TEXT NULL,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Pagos_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 9. BecaEventos
CREATE TABLE IF NOT EXISTS BecaEventos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventoId INT NOT NULL,
    NombreCampana VARCHAR(200) NOT NULL,
    TipoDescuento VARCHAR(50) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    CantidadTotalCodigos INT NOT NULL DEFAULT 1,
    FechaVencimiento DATETIME NULL,
    Acumulable TINYINT(1) NOT NULL DEFAULT 0,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_BecaEventos_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 10. BecaCodigos
CREATE TABLE IF NOT EXISTS BecaCodigos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BecaEventoId INT NOT NULL,
    Codigo VARCHAR(50) NOT NULL,
    Usado TINYINT(1) NOT NULL DEFAULT 0,
    FechaUso DATETIME NULL,
    InscripcionId INT NULL,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_BecaCodigos_BecaEventos FOREIGN KEY (BecaEventoId) REFERENCES BecaEventos(Id),
    CONSTRAINT FK_BecaCodigos_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
    UNIQUE KEY UQ_BecaCodigos_Codigo (Codigo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 11. Contactos
CREATE TABLE IF NOT EXISTS Contactos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(200) NOT NULL,
    Asunto VARCHAR(200) NOT NULL,
    Mensaje TEXT NOT NULL,
    FechaEnvio DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    Leido TINYINT(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 12. Usuarios
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(200) NOT NULL,
    NombreCompleto VARCHAR(200) NOT NULL,
    Email VARCHAR(200) NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    UNIQUE KEY UQ_Usuarios_Username (Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 13. Promociones
CREATE TABLE IF NOT EXISTS Promociones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(200) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    TipoAlumnoId INT NULL,
    CantidadCursosRequeridos INT NOT NULL DEFAULT 1,
    PeriodoMeses INT NOT NULL DEFAULT 12,
    TipoDescuento VARCHAR(20) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    Acumulable TINYINT(1) NOT NULL DEFAULT 0,
    FechaVigenciaDesde DATETIME NOT NULL,
    FechaVigenciaHasta DATETIME NOT NULL,
    DiasValidezCupon INT NOT NULL DEFAULT 90,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Promociones_TiposAlumno FOREIGN KEY (TipoAlumnoId) REFERENCES TiposAlumno(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 14. PromocionCupones
CREATE TABLE IF NOT EXISTS PromocionCupones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PromocionId INT NOT NULL,
    Documento VARCHAR(50) NOT NULL,
    Codigo VARCHAR(10) NOT NULL,
    TipoDescuento VARCHAR(20) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    Acumulable TINYINT(1) NOT NULL DEFAULT 0,
    Usado TINYINT(1) NOT NULL DEFAULT 0,
    FechaUso DATETIME NULL,
    InscripcionDestinoId INT NULL,
    FechaVencimiento DATETIME NULL,
    CreatedBy VARCHAR(100) NULL,
    UpdatedBy VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    DeletedAt DATETIME NULL,
    UNIQUE KEY IX_PromocionCupones_Codigo (Codigo),
    CONSTRAINT FK_PromCup_Promociones FOREIGN KEY (PromocionId) REFERENCES Promociones(Id),
    CONSTRAINT FK_PromCup_InscDest FOREIGN KEY (InscripcionDestinoId) REFERENCES Inscripciones(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 15. PromocionCuponInscripciones
CREATE TABLE IF NOT EXISTS PromocionCuponInscripciones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PromocionCuponId INT NOT NULL,
    InscripcionId INT NOT NULL,
    CONSTRAINT FK_PCI_PromocionCupones FOREIGN KEY (PromocionCuponId) REFERENCES PromocionCupones(Id),
    CONSTRAINT FK_PCI_Inscripciones FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
    INDEX IX_PCI_PromocionCuponId (PromocionCuponId),
    INDEX IX_PCI_InscripcionId (InscripcionId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 16. Articulos
CREATE TABLE IF NOT EXISTS Articulos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CodArticu VARCHAR(50) NOT NULL,
    Descripcio VARCHAR(200) NOT NULL,
    UNIQUE KEY UQ_Articulos_CodArticu (CodArticu)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 17. Clientes
CREATE TABLE IF NOT EXISTS Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    RazonSoci VARCHAR(200) NOT NULL,
    Domicilio VARCHAR(200) NULL,
    CodPostal VARCHAR(20) NULL,
    CodProvin VARCHAR(10) NULL,
    UNIQUE KEY UQ_Clientes_Cuit (Cuit)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 18. Provincias
CREATE TABLE IF NOT EXISTS Provincias (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(10) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    UNIQUE KEY UQ_Provincias_Codigo (Codigo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 19. ResumenCuenta
CREATE TABLE IF NOT EXISTS ResumenCuenta (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    TComp VARCHAR(10) NOT NULL,
    NComp VARCHAR(20) NOT NULL,
    FechaVto DATE NOT NULL,
    Saldo DECIMAL(18,2) NOT NULL,
    UNIQUE KEY UQ_ResumenCuenta (Cuit, TComp, NComp, FechaVto),
    INDEX IX_ResumenCuenta_Cuit (Cuit)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
