namespace UrlShortener.Models;

public class Url
{
    public const string ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int URL_SHORTED_LENGTH = 6;

    public required string ShortedUrl { get; set; }
    public required string OriginalUrl { get; set; }

    public static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
