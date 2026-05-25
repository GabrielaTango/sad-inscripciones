-- Migration: flag de sincronización del débito automático MySQL → Tango.
-- Valores: 'Sincronizado' (default), 'PendienteAlta', 'PendienteBaja', 'PendienteModificacion'.
-- El SyncService toma los != 'Sincronizado' y los empuja a GVA14.CAMPOS_ADICIONALES.
ALTER TABLE Clientes
    ADD COLUMN EstadoSyncDebito VARCHAR(25) NOT NULL DEFAULT 'Sincronizado',
    ADD INDEX IX_Clientes_EstadoSyncDebito (EstadoSyncDebito);
