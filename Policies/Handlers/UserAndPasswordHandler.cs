using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using UrlShortener.Models;
using UrlShortener.Options;
using UrlShortener.Policies.Validation;

namespace UrlShortener.Policies.Handlers;

public class UserAndPasswordHandler : AuthenticationHandler<UserAndPasswordAuthenticationOptions>
{
    private readonly IUserAndPasswordValidation _userAndPasswordValidation;

    public UserAndPasswordHandler(IOptionsMonitor<UserAndPasswordAuthenticationOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, IUserAndPasswordValidation userAndPasswordValidation) : base(options, logger, encoder)
    {
        _userAndPasswordValidation = userAndPasswordValidation;
    }
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return AuthenticateResult.NoResult();

        User? user;
        try
        {
            user = await Request.ReadFromJsonAsync<User>();
        }
        catch
        {
            return AuthenticateResult.Fail("The json body of the request is not valid");
        }

        bool isValidUsernameAndPassword = await _userAndPasswordValidation.IsValidUserAndPassword(user);
        if (!isValidUsernameAndPassword)
            return AuthenticateResult.Fail("Username or password incorrect");

        var claims = new List<Claim>()
        {
            new("userId", user!.Id.ToString()),
            new("newPassword", user!.NewPassword ?? "")
        };
        var claimsIdentity = new ClaimsIdentity(claims, this.Scheme.Name, "userId", ClaimTypes.Role);
        var claimsPrincipal = new ClaimsPrincipal (claimsIdentity);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claimsPrincipal), this.Scheme.Name));
    }
}