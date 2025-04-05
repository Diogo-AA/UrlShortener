using UrlShortener.Models;

namespace UrlShortener.Data;

public interface IRepository
{
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
    Task<ApiKey.ValidationStatus> ValidateApiKey(Guid apiKey);
    Task<bool> CreateShortedUrl(Guid apiKey, Url originalUrl);
    Task<string?> GetOriginalUrl(string shortedUrlId);
    Task<string?> GetOriginalUrl(Guid userId, string shortedUrlId);
    Task<bool> RemoveUrl(Guid apiKey, string shortedUrlId);
    Task<string?> GetUrlFromUser(Guid apiKey, string shortedUrlId);
    Task<IEnumerable<Url>> GetAllUrlsFromUser(Guid apiKey, Func<string, string> GetShortedUrl, int limit);
    Task<bool> LogError(string traceId, string endpoint, Exception exception);
}