using Microsoft.Extensions.Caching.Distributed;

namespace UrlShortener.Options;

public class CacheOptions
{
    private const int ABSOLUTE_EXPIRATION_RELATIVE_HOURS = 1;
    private const int SLIDING_EXPIRATION_MINUTES = 5;
 
    public static readonly DistributedCacheEntryOptions Options = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ABSOLUTE_EXPIRATION_RELATIVE_HOURS),
        SlidingExpiration = TimeSpan.FromMinutes(SLIDING_EXPIRATION_MINUTES)
    };
}