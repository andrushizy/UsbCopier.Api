using System.Security.Cryptography;

namespace UsbCopier.Api.Services;

/// <summary>
/// Хеширование паролей через PBKDF2 (HMAC-SHA256). Формат хранения:
/// "PBKDF2.{iterations}.{salt-base64}.{hash-base64}".
///
/// PBKDF2 встроен в .NET — не требует NuGet-пакетов. Для дипломной достаточно;
/// в продакшене лучше Argon2id.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        return $"PBKDF2.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 4 || parts[0] != "PBKDF2") return false;
        if (!int.TryParse(parts[1], out var iter)) return false;
        byte[] salt, hash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch { return false; }

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, iter, HashAlgorithmName.SHA256);
        var test = pbkdf2.GetBytes(hash.Length);
        return CryptographicOperations.FixedTimeEquals(test, hash);
    }
}
