namespace UrlShortener.Models;

public class Url
{
    public const string ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int URL_SHORTED_LENGTH = 6;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string ShortedUrl { get; set; }
    public required string OriginalUrl { get; set; }

    public static bool IsValidUrl(string url)
    {
        Uri uriResult;
        return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
