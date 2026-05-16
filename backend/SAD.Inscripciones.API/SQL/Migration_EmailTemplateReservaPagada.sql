-- Migration: seed template "reserva-pagada"
-- Se dispara cuando una inscripcion pasa a estado "Reservada"
-- (el alumno pagó el MontoReserva, todavía debe el saldo).
-- El BodyHtml real lo carga el backend desde EmailTemplates/reserva-pagada.html
-- la primera vez que se accede.
INSERT INTO EmailTemplates (Codigo, Nombre, Asunto, BodyHtml, BodyJson, Activo)
SELECT 'reserva-pagada',
       'Reserva pagada',
       'Reserva confirmada - {{Evento}}',
       '',
       NULL,
       1
WHERE NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE Codigo = 'reserva-pagada');
