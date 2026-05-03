namespace SAD.Inscripciones.API.Services.Interfaces;

public interface ICryptoService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
