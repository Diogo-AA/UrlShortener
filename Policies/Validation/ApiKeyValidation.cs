using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Policies.Validation;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IRepository _repository;

    public ApiKeyValidation(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiKey.ValidationStatus> IsValidApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ApiKey.ValidationStatus.Invalid;

        if (!Guid.TryParse(apiKey, out Guid parsedApiKey))
            return ApiKey.ValidationStatus.Invalid;

        var validationResult = await _repository.ValidateApiKey(parsedApiKey);

        return validationResult;
    }
}