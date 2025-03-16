using Microsoft.AspNetCore.Authentication;

namespace UrlShortener.Policies.Options;

public class UserAndPasswordAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "UserAndPassword";
    public const string DefaultPolicy = "UserAndPasswordPolicy";
}