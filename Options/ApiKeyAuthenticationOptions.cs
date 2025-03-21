using Microsoft.AspNetCore.Authentication;

namespace UrlShortener.Options;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string DefaultPolicy = "ApiKeyPolicy";
    public const string HeaderName = "x-api-key";
}