namespace UrlShortener.Models;

public class Url
{
    public const string ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
    public const int URL_SHORTED_LENGTH = 6;
    private const int MIN_URL_SHORTED_LENGTH = 6;

    public required string ShortedUrl { get; set; }
    public required string ShortedUrlId { get; set; }
    public required string OriginalUrl { get; set; }

    public static bool IsValidUrl(Uri url)
    {
        return url.IsAbsoluteUri && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
    }

    public static bool IsValidShortedUrlId(string shortedUrlId)
    {
        return !string.IsNullOrWhiteSpace(shortedUrlId) & shortedUrlId.Length >= MIN_URL_SHORTED_LENGTH 
            & shortedUrlId.Length <= URL_SHORTED_LENGTH;
    }
}
