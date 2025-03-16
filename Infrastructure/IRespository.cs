using UrlShortener.Models;

namespace UrlShortener.Infrastructure;

public interface IRepository
{
    public const int LIMIT_URLS_SHOWN = 100;
    public const int DEFAULT_URLS_SHOWN = 20;

    public void Initialize();
    Task<User?> GetUser(string username);
    Task<User?> GetUser(Guid apiKey);
    Task<bool> IsUsernameInUse(string username);
    Task<Guid?> CreateUser(User user);
    Task<bool> UpdateUserPassword(User user);
    Task<bool> VerifyUser(User user);
    Task<bool> RemoveUser(Guid userId);
    Task<bool> RemoveUser(string username);
    Task<DateTime?> GetLastTimeApiKeyUpdated(Guid userId);
    Task<Guid?> UpdateApiKey(Guid userId);
    Task<Guid?> GetApiKey(Guid userId);
    Task<bool> ValidateApiKey(Guid apiKey);
    Task<string?> CreateShortedUrl(Guid apiKey, Uri originalUrl);
    Task<string?> GetOriginalUrl(string shortedUrlId);
    Task<string?> GetOriginalUrl(Guid userId, string shortedUrlId);
    Task<bool> RemoveUrl(Guid apiKey, string shortedUrlId);
    Task<IEnumerable<Url>> GetAllUrlsFromUser(Guid apiKey, int limit);
    Task<bool> LogError(string traceId, string endpoint, Exception exception);
}