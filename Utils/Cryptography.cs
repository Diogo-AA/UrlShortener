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

    public static string CreateShortedUrl(string originalUrl)
    {
        string shortedUrl = "";
        byte[] originalUrlBytes = Encoding.UTF8.GetBytes(originalUrl);
        ushort originalUrlValue = BitConverter.ToUInt16(originalUrlBytes, 0);
        
        var rng = RandomNumberGenerator.Create();
        for (int i = 0; i < UrlShorted.URL_SHORTED_LENGTH; i++)
        {
            byte[] data = new byte[UrlShorted.URL_SHORTED_LENGTH];
            rng.GetBytes(data, 0, UrlShorted.URL_SHORTED_LENGTH);

            int randomNumber = originalUrlValue + BitConverter.ToUInt16(data, 0);
            int index = randomNumber % UrlShorted.ALLOWED_CHARS.Length;
            shortedUrl += UrlShorted.ALLOWED_CHARS[index];
        }

        return shortedUrl;
    }
}
