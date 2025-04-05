using Microsoft.Extensions.Caching.Distributed;
using UrlShortener.Data;
using UrlShortener.Factories;
using UrlShortener.Models;
using UrlShortener.Options;

namespace UrlShortener.Services;

public class ShortenerService : IShortenerService
{
    private readonly IRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly UrlModelFactory _urlModelFactory;

    public ShortenerService(IRepository repository, IDistributedCache cache, UrlModelFactory urlModelFactory)
    {
        _repository = repository;
        _cache = cache;
        _urlModelFactory = urlModelFactory;
    }

    #region  User
    public async Task<Guid?> CreateUserAsync(User user)
    {
        bool isUsernameInUse = await _repository.IsUsernameInUse(user.Username!);
        if (isUsernameInUse)
            return null;

        Guid? apiKey = await _repository.CreateUser(user);
        if (!apiKey.HasValue)
            throw new Exception("Error creating the user");

        return apiKey.Value;
    }

    public async Task<bool> UpdateUserPasswordAsync(User user)
    {
        return await _repository.UpdateUserPassword(user);
    }

    public async Task<bool> RemoveUserAsync(Guid userId)
    {
        return await _repository.RemoveUser(userId);
    }
#endregion

#region ApiKey
    public async Task<Guid?> GetApiKeyAsync(Guid userId)
    {
        return await _repository.GetApiKey(userId);
    }

    public async Task<DateTime?> GetLastTimeApiKeyUpdatedAsync(Guid userId)
    {
        return await _repository.GetLastTimeApiKeyUpdated(userId);
    }

    public async Task<Guid?> UpdateApiKeyAsync(Guid userId)
    {
        return await _repository.UpdateApiKey(userId);
    }

#endregion

#region  Url
    public async Task<Url?> CreateShortedUrlAsync(Guid apiKey, Uri url)
    {
        Url shortedUrl = _urlModelFactory.Create(url.AbsoluteUri);

        bool shortedUrlCreated = await _repository.CreateShortedUrl(apiKey, shortedUrl);

        return shortedUrlCreated ? shortedUrl : null;
    }

    public async Task<bool> RemoveUrlAsync(Guid apiKey, string shortedUrlId)
    {
        bool removed = await _repository.RemoveUrl(apiKey, shortedUrlId);
        if (removed)
            await _cache.RemoveAsync(shortedUrlId);

        return removed;
    }

    public async Task<IEnumerable<Url>> GetUrlsAsync(Guid apiKey, int limit = IShortenerService.LIMIT_URLS_SHOWN)
    {
        return await _repository.GetAllUrlsFromUser(apiKey, _urlModelFactory.GetShortedUrl, limit);
    }

    public async Task<Url?> GetUrlAsync(Guid apiKey, string shortedUrlId)
    {
        string? originalUrl = await _repository.GetUrlFromUser(apiKey, shortedUrlId);
        if (string.IsNullOrEmpty(originalUrl))
            return null;

        return _urlModelFactory.Create(originalUrl, shortedUrlId);
    }

    public async Task<string?> GetOriginalUrlAsync(string shortedUrlId)
    {
        string? url = await _cache.GetStringAsync(shortedUrlId);
        if (string.IsNullOrEmpty(url))
        {
            url = await _repository.GetOriginalUrl(shortedUrlId);

            if (!string.IsNullOrEmpty(url))
                await _cache.SetStringAsync(shortedUrlId, url, CacheOptions.Options);
        }
        
        return url;
    }
    #endregion
}