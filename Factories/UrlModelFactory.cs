using UrlShortener.Models;
using UrlShortener.Utils;

namespace UrlShortener.Factories;

public class UrlModelFactory
{
    private readonly string _scheme;
    private readonly string _hostname;

    public UrlModelFactory(string scheme, string hostname)
    {
        _scheme = scheme;
        _hostname = hostname;
    }

    public string GetShortedUrl(string shortedUrlId)
    {
        return $"{_scheme}://{_hostname}/{shortedUrlId}";
    }

    public Url Create(string originalUrl)
    {
        string shortedUrlId = Cryptography.GenerateShortUrl();

        return new Url()
        {
            OriginalUrl = originalUrl,
            ShortedUrlId = shortedUrlId,
            ShortedUrl = GetShortedUrl(shortedUrlId)
        };
    }

    public Url Create(string originalUrl, string shortedUrlId)
    {
        return new Url()
        {
            OriginalUrl = originalUrl,
            ShortedUrlId = shortedUrlId,
            ShortedUrl = GetShortedUrl(shortedUrlId)
        };
    }
}