namespace UrlShortener.Options;

public class RateLimitingOptions
{
    public const int PERMIT_LIMIT = 20;
    public const int QUEUE_LIMIT = 0;
    public const int WINDOW_SECONDS = 60;
}