-- Deduplicar Pagos con mismo (InscripcionId, ReferenciaExterna): mantener Id menor
DELETE p1
FROM Pagos p1
INNER JOIN Pagos p2
  ON p1.InscripcionId = p2.InscripcionId
  AND p1.ReferenciaExterna = p2.ReferenciaExterna
WHERE p1.Id > p2.Id
  AND p1.ReferenciaExterna IS NOT NULL;

-- UNIQUE: en MySQL los NULL se consideran distintos entre sí,
-- así que múltiples Pagos manuales sin ReferenciaExterna siguen permitidos.
ALTER TABLE Pagos
    ADD CONSTRAINT UK_Pagos_Inscripcion_RefExt UNIQUE (InscripcionId, ReferenciaExterna);
