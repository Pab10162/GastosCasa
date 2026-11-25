using System.Security.Cryptography;

namespace GastosCasa.BLL;

// Hash de contraseñas con PBKDF2 (viene incluido en .NET, no hace falta ningún paquete).
// Idea: nunca se guarda la contraseña. Se guarda "sal + hash". Para validar,
// se vuelve a calcular el hash con la misma sal y se compara.
public static class Seguridad
{
    private const int Iteraciones = 100_000;   // lento a propósito: dificulta ataques de fuerza bruta
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    public static string Hashear(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string guardado)
    {
        var partes = guardado.Split('.');
        if (partes.Length != 3) return false;

        var iteraciones = int.Parse(partes[0]);
        var sal = Convert.FromBase64String(partes[1]);
        var hashEsperado = Convert.FromBase64String(partes[2]);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
        // Comparación en tiempo constante: no revela "cuántos bytes acertaste".
        return CryptographicOperations.FixedTimeEquals(hash, hashEsperado);
    }
}
