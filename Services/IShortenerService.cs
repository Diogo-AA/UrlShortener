using UrlShortener.Models;

namespace UrlShortener.Services;

public interface IShortenerService 
{
    public const int DEFAULT_URLS_SHOWN = 20;
    public const int LIMIT_URLS_SHOWN = 100;

#region  User
    public Task<Guid?> CreateUserAsync(User user);

    public Task<bool> UpdateUserPasswordAsync(User user);

    public Task<bool> RemoveUserAsync(Guid userId);
#endregion

#region ApiKey
    public Task<Guid?> GetApiKeyAsync(Guid userId);

    public Task<DateTime?> GetLastTimeApiKeyUpdatedAsync(Guid userId);

    public Task<Guid?> UpdateApiKeyAsync(Guid userId);

#endregion

#region  Url
    public Task<Url?> CreateShortedUrlAsync(Guid apiKey, Uri url);

    public Task<bool> RemoveUrlAsync(Guid apiKey, string shortedUrlId);

    public Task<IEnumerable<Url>> GetUrlsAsync(Guid apiKey, int limit = LIMIT_URLS_SHOWN);

    public Task<Url?> GetUrlAsync(Guid apiKey, string shortedUrlId);

    public Task<string?> GetOriginalUrlAsync(string shortedUrlId);
#endregion
}