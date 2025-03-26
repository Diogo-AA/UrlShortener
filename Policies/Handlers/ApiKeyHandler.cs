using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using UrlShortener.Options;
using UrlShortener.Policies.Validation;

namespace UrlShortener.Policies.Handlers;

public class ApiKeyHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidation _apiKeyValidation;
    private string? FailureMessage;
    
    public ApiKeyHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, IApiKeyValidation apiKeyValidation) : base(options, logger, encoder)
    {
        _apiKeyValidation = apiKeyValidation;
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!string.IsNullOrEmpty(FailureMessage))
        {
            var responseFeature = Response.HttpContext.Features.Get<IHttpResponseFeature>();
            if (responseFeature is not null)
            {
                responseFeature.ReasonPhrase = FailureMessage;
                
                if (Context.Items.TryGetValue("statusCode", out object? statusCodeObj) && statusCodeObj is int statusCode)
                {
                    responseFeature.StatusCode = statusCode;
                }
            }
        }

        return Task.CompletedTask;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return AuthenticateResult.NoResult();

        bool? headerExists = Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKey);
        if (!headerExists.GetValueOrDefault())
        {
            Context.Items.Add("statusCode", StatusCodes.Status401Unauthorized);
            FailureMessage = $"Header: '{ApiKeyAuthenticationOptions.HeaderName}' not found.";
            return AuthenticateResult.Fail(FailureMessage);
        }

        var validationResult = await _apiKeyValidation.IsValidApiKey(apiKey!);
        switch (validationResult)
        {
            case Models.ApiKey.ValidationStatus.Invalid:
                FailureMessage = "Invalid API key";
                Context.Items.Add("statusCode", StatusCodes.Status401Unauthorized);
                return AuthenticateResult.Fail(FailureMessage);
            case Models.ApiKey.ValidationStatus.Expired:
                FailureMessage = "Expired API key";
                Context.Items.Add("statusCode", StatusCodes.Status403Forbidden);
                return AuthenticateResult.Fail(FailureMessage);
            default:
                var claims = new List<Claim>()
                {
                    new(ApiKeyAuthenticationOptions.HeaderName, apiKey!)
                };
                var claimsIdentity = new ClaimsIdentity(claims, this.Scheme.Name, ApiKeyAuthenticationOptions.HeaderName, ClaimTypes.Role);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claimsPrincipal), this.Scheme.Name));
        }
    }
}