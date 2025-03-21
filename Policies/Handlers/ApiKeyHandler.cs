using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using UrlShortener.Options;
using UrlShortener.Policies.Validation;

namespace UrlShortener.Policies.Handlers;

public class ApiKeyHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidation _apiKeyValidation;
    
    public ApiKeyHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, IApiKeyValidation apiKeyValidation) : base(options, logger, encoder)
    {
        _apiKeyValidation = apiKeyValidation;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return AuthenticateResult.NoResult();

        bool? headerExists = Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKey);
        if (!headerExists.GetValueOrDefault())
            return AuthenticateResult.Fail($"Header: '{ApiKeyAuthenticationOptions.HeaderName}' not found.");

        bool isValidApiKey = await _apiKeyValidation.IsValidApiKey(apiKey!);        
        if (!isValidApiKey)
            return AuthenticateResult.Fail("Invalid or expired API key");

        var claims = new List<Claim>()
        {
            new(ApiKeyAuthenticationOptions.HeaderName, apiKey!)
        };
        var claimsIdentity = new ClaimsIdentity(claims, this.Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claimsPrincipal), this.Scheme.Name));
    }
}