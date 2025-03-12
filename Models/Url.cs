namespace UrlShortener.Models;

public class Url
{
    public const string ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int URL_SHORTED_LENGTH = 6;

    public required string ShortedUrl { get; set; }
    public required string OriginalUrl { get; set; }

    public static bool IsValidUrl(Uri url)
    {
        return url.IsAbsoluteUri && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
    }
}
