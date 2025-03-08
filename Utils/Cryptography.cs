using System.Text;
using System.Security.Cryptography;
using UrlShortener.Models;

namespace UrlShortener.Utils;

public class Cryptography
{
    private const BCrypt.Net.HashType HASH_TYPE = BCrypt.Net.HashType.SHA512;

    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, HASH_TYPE);
    }

    public static bool VerifyPassword(string passwordProvided, string passwordHashed)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(passwordProvided, passwordHashed, HASH_TYPE);
    }

    public static string GenerateShortUrl()
    {
        string shortedUrl = "";
        
        var rng = RandomNumberGenerator.Create();
        for (int i = 0; i < Url.URL_SHORTED_LENGTH; i++)
        {
            byte[] randomData = new byte[Url.URL_SHORTED_LENGTH];
            rng.GetBytes(randomData, 0, Url.URL_SHORTED_LENGTH);

            int index = BitConverter.ToUInt16(randomData, 0) % Url.ALLOWED_CHARS.Length;
            shortedUrl += Url.ALLOWED_CHARS[index];
        }

        return shortedUrl;
    }
}
