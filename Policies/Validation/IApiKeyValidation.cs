namespace UrlShortener.Policies.Validation;

public interface IApiKeyValidation
{
    Task<bool> IsValidApiKey(string apiKey);
}