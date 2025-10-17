using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace Katana.Infrastructure.Utils;

public static class EncryptionHelper
{
    public static string CreateHash(string input, string salt)
    {
        var saltBytes = Encoding.UTF8.GetBytes(salt);
        var hash = KeyDerivation.Pbkdf2(
            password: input,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(hash);
    }

    public static string GenerateSalt()
    {
        var salt = new byte[128 / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }

    public static bool VerifyHash(string input, string salt, string hash)
    {
        var computedHash = CreateHash(input, salt);
        return computedHash == hash;
    }

    public static string CreateMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public static string CreateSHA256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public static string CreateHMACSignature(string message, string secretKey)
    {
        var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        using var hmac = new HMACSHA256(secretKeyBytes);
        var signatureBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(signatureBytes);
    }

    public static bool VerifyHMACSignature(string message, string secretKey, string signature)
    {
        var computedSignature = CreateHMACSignature(message, secretKey);
        return computedSignature == signature;
    }
}

