using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortener.Options;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/user")]
[Authorize(Policy = UserAndPasswordAuthenticationOptions.DefaultPolicy)]
public class UserController : ControllerBase
{
    private readonly IRepository _repository;

    public UserController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] User userRequest)
    {
        bool isUsernameInUse = await _repository.IsUsernameInUse(userRequest.Username!);
        if (isUsernameInUse)
            return Conflict("Username is already in use.");

        Guid? apiKey = await _repository.CreateUser(userRequest);
        if (!apiKey.HasValue)
            return Problem("Error creating the user. Try again later.");

        string uri = $"{Request.Scheme}:// {Request.Host}{Request.Path}";
        return Created(uri, apiKey.Value);
    }

    [HttpPatch("update-password")]
    public async Task<IActionResult> UpdatePassword()
    {
        Guid userId = GetUserIdFromClaims();
        string? newPassword = GetNewPasswordFromClaims();
            
        bool passwordUpdated = await _repository.UpdateUserPassword(new Models.User() { Id = userId, NewPassword = newPassword });
        if (!passwordUpdated)
            return Problem("Error updating the password. Make sure you use the right password.");

        return NoContent();
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete()
    {
        Guid userId = GetUserIdFromClaims();
        
        bool userRemoved = await _repository.RemoveUser(userId);
        if (!userRemoved)
            return Problem("Error removing the user. Try again later.");

        return NoContent();
    }

    private Guid GetUserIdFromClaims()
    {
        var claim = User.Claims.Where(claim => claim.Type == "userId").FirstOrDefault();
        if (!Guid.TryParse(claim?.Value, out Guid userId))
            throw new AuthenticationFailureException("Error getting the userId");
        return userId;
    }

    private string? GetNewPasswordFromClaims()
    {
        var claim = User.Claims.Where(claim => claim.Type == "newPassword").FirstOrDefault();
        return claim?.Value;
    }
}
