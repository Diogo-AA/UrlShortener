using UrlShortener.Models;

namespace UrlShortener.Policies.Validation;

public interface IUserAndPasswordValidation
{
    Task<bool> IsValidUserAndPassword(User? user);
}