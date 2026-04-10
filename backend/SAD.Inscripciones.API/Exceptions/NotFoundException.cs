namespace SAD.Inscripciones.API.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, int id) : base($"{entity} con Id {id} no encontrado.") { }
}
