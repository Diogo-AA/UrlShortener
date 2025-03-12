using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly IRepository _repository;

    public UserController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] User userRequest)
    {
        bool isUsernameInUse = await _repository.IsUsernameInUse(userRequest.Username!);
        if (isUsernameInUse)
            return Conflict("Username is already in use.");

        Guid? apiKey = await _repository.CreateUser(userRequest);
        if (!apiKey.HasValue)
            return StatusCode(StatusCodes.Status500InternalServerError, "Error creating the user. Try again later.");

        return Ok($"User created succesfully. Your API Key is: {apiKey.Value}");
    }

    [HttpPost("update-password")]
    public async Task<IActionResult> UpdatePassword([FromBody] User userRequest)
    {
        bool passwordUpdated = await _repository.UpdateUserPassword(userRequest);
        if (!passwordUpdated)
            return StatusCode(StatusCodes.Status500InternalServerError, "Error updating the password. Make sure you use the right password.");

        return Ok();
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] User userRequest)
    {
        bool validCredentials = await _repository.VerifyUser(userRequest);
        if (!validCredentials)
            return BadRequest("Username or password incorrect.");

        bool userRemoved = await _repository.RemoveUser(userRequest.Username!);
        if (!userRemoved)
            return StatusCode(StatusCodes.Status500InternalServerError, "Error removing the user. Try again later.");

        return Ok();
    }
}
