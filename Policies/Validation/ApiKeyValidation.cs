using UrlShortener.Data;

namespace UrlShortener.Policies.Validation;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IRepository _repository;

    public ApiKeyValidation(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsValidApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        if (!Guid.TryParse(apiKey, out Guid parsedApiKey))
            return false;

        bool isValidApiKey = await _repository.ValidateApiKey(parsedApiKey);

        return isValidApiKey;
    }
}