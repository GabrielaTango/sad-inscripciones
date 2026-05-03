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

-- 20. Vendedores (mirror de GVA23 con cuentas de tesorería del capítulo)
CREATE TABLE IF NOT EXISTS Vendedores (
    CodVended VARCHAR(20) NOT NULL PRIMARY KEY,
    CtaCaja INT NOT NULL DEFAULT 0,
    CtaTransferencia INT NOT NULL DEFAULT 0,
    CtaCuotas INT NOT NULL DEFAULT 0,
    CtaOtra INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 21. PagosCuentaCorriente (recibos de resumen de cuenta — MP del socio o cobro presencial del capítulo)
CREATE TABLE IF NOT EXISTS PagosCuentaCorriente (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cuit VARCHAR(20) NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    Comprobantes TEXT NULL,                  -- JSON array; NULL/'[]' = pago a cuenta
    ExternalReference VARCHAR(100) NOT NULL,
    PreferenceId VARCHAR(100) NULL,
    EstadoPago VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
    MpPaymentId BIGINT NULL,
    FechaPago DATETIME NULL,
    SincronizadoTango TINYINT(1) NOT NULL DEFAULT 0,
    MontoImputado DECIMAL(18,2) NULL,
    CodVended VARCHAR(20) NULL,
    MedioPago VARCHAR(20) NULL,
    CtaTesoreria INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX IX_PagoCC_Cuit (Cuit),
    INDEX IX_PagoCC_ExtRef (ExternalReference),
    INDEX IX_PagoCC_SincTango (EstadoPago, SincronizadoTango),
    INDEX IX_PagoCC_CodVended (CodVended)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 22. ConfiguracionEmail (singleton: SMTP transaccional)
CREATE TABLE IF NOT EXISTS ConfiguracionEmail (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Host VARCHAR(255) NOT NULL DEFAULT '',
    Port INT NOT NULL DEFAULT 587,
    EnableSsl TINYINT(1) NOT NULL DEFAULT 1,
    Usuario VARCHAR(255) NOT NULL DEFAULT '',
    PasswordCifrado TEXT NOT NULL,
    FromEmail VARCHAR(255) NOT NULL DEFAULT '',
    FromName VARCHAR(255) NOT NULL DEFAULT 'Inscripciones SAD',
    ReplyTo VARCHAR(255) NULL,
    BccCopia VARCHAR(500) NULL,
    Asunto VARCHAR(500) NOT NULL DEFAULT 'Confirmación de inscripción - {{Evento}}',
    Activo TINYINT(1) NOT NULL DEFAULT 0,
    IgnorarCertificadoSsl TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO ConfiguracionEmail (Id, Host, Port, EnableSsl, Usuario, PasswordCifrado, FromEmail, FromName, Asunto, Activo)
SELECT 1, '', 587, 1, '', '', '', 'Inscripciones SAD', 'Confirmación de inscripción - {{Evento}}', 0
WHERE NOT EXISTS (SELECT 1 FROM ConfiguracionEmail WHERE Id = 1);

-- 23. EmailTemplates (templates HTML editables desde el admin)
CREATE TABLE IF NOT EXISTS EmailTemplates (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(80) NOT NULL UNIQUE,
    Nombre VARCHAR(200) NOT NULL,
    Asunto VARCHAR(500) NOT NULL DEFAULT '',
    BodyHtml MEDIUMTEXT NOT NULL,
    BodyJson MEDIUMTEXT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(100) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO EmailTemplates (Codigo, Nombre, Asunto, BodyHtml, BodyJson, Activo)
SELECT 'inscripcion-confirmada',
       'Confirmación de inscripción',
       'Confirmación de inscripción - {{Evento}}',
       '',
       NULL,
       1
WHERE NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Codigo = 'inscripcion-confirmada');

-- 24. SyncTrigger (singleton: flag para forzar Full Sync desde el admin)
CREATE TABLE IF NOT EXISTS SyncTrigger (
    Id INT NOT NULL PRIMARY KEY,
    RequestedAt DATETIME NULL,
    RequestedBy VARCHAR(100) NULL,
    ConsumedAt DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO SyncTrigger (Id, RequestedAt, RequestedBy, ConsumedAt) VALUES (1, NULL, NULL, NULL);

-- ============================================================
-- Columnas y constraints agregadas a tablas de la base original
-- (correspondientes a Migration_PagosSincronizadoTango, _PagosUniqueRefExt,
--  _UsuariosCapitulo, _ClientesCodVended, y la columna SincronizadoTango de
--  Inscripciones consumida por SyncController).
-- ============================================================

ALTER TABLE Pagos ADD COLUMN SincronizadoTango TINYINT(1) NOT NULL DEFAULT 0;
CREATE INDEX IX_Pagos_SincronizadoTango ON Pagos (EstadoPago, SincronizadoTango, DeletedAt);
ALTER TABLE Pagos ADD CONSTRAINT UK_Pagos_Inscripcion_RefExt UNIQUE (InscripcionId, ReferenciaExterna);

ALTER TABLE Inscripciones ADD COLUMN SincronizadoTango TINYINT(1) NOT NULL DEFAULT 0;
CREATE INDEX IX_Inscripciones_SincronizadoTango ON Inscripciones (Estado, SincronizadoTango, DeletedAt);

ALTER TABLE Clientes ADD COLUMN CodVended VARCHAR(20) NULL;
CREATE INDEX IX_Clientes_CodVended ON Clientes (CodVended);

ALTER TABLE Usuarios
    ADD COLUMN CodVended VARCHAR(20) NULL,
    ADD COLUMN EsCapitulo TINYINT(1) NOT NULL DEFAULT 0,
    ADD CONSTRAINT FK_Usuarios_Vendedores
        FOREIGN KEY (CodVended) REFERENCES Vendedores(CodVended)
        ON UPDATE CASCADE ON DELETE RESTRICT;
CREATE INDEX IX_Usuarios_CodVended ON Usuarios (CodVended);
