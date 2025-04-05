using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Policies.Validation;

public class UserAndPasswordValidation : IUserAndPasswordValidation
{
    private readonly IRepository _repository;

    public UserAndPasswordValidation(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsValidUserAndPassword(User? user)
    {
        if (!User.IsValid(user))
            return false;

        bool isValidUser = await _repository.VerifyUser(user!);

        return isValidUser;
    }
}