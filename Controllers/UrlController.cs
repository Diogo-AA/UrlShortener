using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;

namespace UrlShortener.Controllers;

[ApiController]
[Route("url")]
public class UrlController : ControllerBase
{
    private readonly DbController _dbController;

    public UrlController(DbController dbController)
    {
        _dbController = dbController;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] string url)
    {
        bool validUrl = Models.Url.IsValidUrl(url);
        if (!validUrl)
            return BadRequest("Invalid url.");

        bool validApiKey = await _dbController.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        string? shortedUrl = await _dbController.CreateShortedUrl(apiKey, url);
        if (string.IsNullOrEmpty(shortedUrl))
            return Problem("Error creating the shorted url. Try again later.");

        return Ok(shortedUrl);
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] string url)
    {
        bool validApiKey = await _dbController.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        bool removed = await _dbController.RemoveUrl(apiKey, url);
        if (!removed)
            return Problem("Error removing the shorted url. Try again later.");

        return Ok();
    }

    [HttpPost("get")]
    public async Task<IActionResult> Get([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] int limit = DbController.DEFAULT_URLS_SHOWN)
    {
        if (limit < 0 || limit > DbController.LIMIT_URLS_SHOWN)
            return BadRequest($"The limit of the results shown must be between 0 and {DbController.LIMIT_URLS_SHOWN}");

        bool validApiKey = await _dbController.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        var urls = await _dbController.GetAllUrlsFromUser(apiKey, limit);

        return Ok(urls);
    }
}
