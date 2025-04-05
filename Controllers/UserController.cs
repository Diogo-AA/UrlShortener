using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Options;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/user")]
[Authorize(Policy = UserAndPasswordAuthenticationOptions.DefaultPolicy)]
public class UserController : ControllerBase
{
    private readonly IShortenerService _service;

    public UserController(IShortenerService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] User userRequest)
    {
        if (!Models.User.IsValid(userRequest))
            return BadRequest("Username or password are not valid.");

        Guid? apiKey = await _service.CreateUserAsync(userRequest);
        if (!apiKey.HasValue)
            return Conflict("Username is already in use.");

        string createdAt = $"{Request.Scheme}:// {Request.Host}{Request.Path}";
        return Created(createdAt, apiKey.Value);
    }

    [HttpPatch("update-password")]
    public async Task<IActionResult> UpdatePassword()
    {
        Guid userId = GetUserIdFromClaims();
        string? newPassword = GetNewPasswordFromClaims();

        if (string.IsNullOrWhiteSpace(newPassword))
            return BadRequest("New password can't be empty.");
            
        bool passwordUpdated = await _service.UpdateUserPasswordAsync(new Models.User() { Id = userId, NewPassword = newPassword });
        if (!passwordUpdated)
            return Problem("Error updating the password.");

        return NoContent();
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete()
    {
        Guid userId = GetUserIdFromClaims();
        
        bool userRemoved = await _service.RemoveUserAsync(userId);
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
