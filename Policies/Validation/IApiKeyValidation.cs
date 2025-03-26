using UrlShortener.Models;

namespace UrlShortener.Policies.Validation;

public interface IApiKeyValidation
{
    Task<ApiKey.ValidationStatus> IsValidApiKey(string apiKey);
}