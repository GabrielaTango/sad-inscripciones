-- Migration: Tabla SyncTrigger
-- Singleton (Id=1) que el SyncService consulta para saber si debe correr un FullSync
-- bajo demanda. RequestedAt se setea cuando un admin pide sincronizar; el Worker lo
-- toma, lo limpia (lo copia a ConsumedAt) y ejecuta FullSyncAsync.
CREATE TABLE IF NOT EXISTS SyncTrigger (
    Id INT NOT NULL PRIMARY KEY,
    RequestedAt DATETIME NULL,
    RequestedBy VARCHAR(100) NULL,
    ConsumedAt DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO SyncTrigger (Id, RequestedAt, RequestedBy, ConsumedAt) VALUES (1, NULL, NULL, NULL);
