-- Token único por inscripción para el external_reference de MercadoPago.
-- Antes el external_reference era el id de inscripción pelado; al recrear la base los ids
-- se reusan y el sandbox de MP (persistente) devolvía pagos viejos de inscripciones anteriores
-- con el mismo id. Con un token único la búsqueda por referencia solo trae los pagos de ESTA
-- inscripción. Formato del external_reference: "{Id}-{PublicRef}".
ALTER TABLE Inscripciones
    ADD COLUMN PublicRef VARCHAR(40) NULL;

-- IMPORTANTE: NO se backfillea. Las inscripciones existentes (sobre todo las pendientes) ya
-- tienen su preferencia de MP creada con external_reference = id pelado ("58"). Si les pusiéramos
-- un token, ValidarInscripcion buscaría "58-token" y nunca encontraría ese pago (está bajo "58"),
-- y la pendiente no se confirmaría. Dejándolas en NULL, ExternalReferenceHelper.Build cae al id
-- pelado y siguen matcheando igual que antes. Solo las inscripciones NUEVAS llevan token.
