using System.Security.Cryptography;
using System.Text;

public class Encriptador
{
    // HASH para Contraseñas (SHA256)
    public static string GenerarHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }

    // ENCRIPTADO SIMÉTRICO (AES) para datos sensibles (ej. Telefono o DNI)
    // Nota: Esto es opcional según qué tan estricto sea el profesor
}