using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;

namespace UrlShortener.Controllers;

[ApiController]
[Route("url")]
public class UrlController : ControllerBase
{
    private readonly IRepository _repository;

    public UrlController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] string url)
    {
        bool validUrl = Models.Url.IsValidUrl(url);
        if (!validUrl)
            return BadRequest("Invalid url.");

        bool validApiKey = await _repository.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        string? shortedUrl = await _repository.CreateShortedUrl(apiKey, url);
        if (string.IsNullOrEmpty(shortedUrl))
            return BadRequest($"The url '{url}' is already shortened.");

        return Ok(shortedUrl);
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] string url)
    {
        bool validApiKey = await _repository.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        bool removed = await _repository.RemoveUrl(apiKey, url);
        if (!removed)
            return Problem("Error removing the shorted url. Try again later.");

        return Ok();
    }

    [HttpPost("get")]
    public async Task<IActionResult> Get([FromHeader(Name = "x-api-key")] Guid apiKey, [FromQuery] int limit = IRepository.DEFAULT_URLS_SHOWN)
    {
        if (limit < 0 || limit > IRepository.LIMIT_URLS_SHOWN)
            return BadRequest($"The limit of the results shown must be between 0 and {IRepository.LIMIT_URLS_SHOWN}");

        bool validApiKey = await _repository.ValidateApiKey(apiKey);
        if (!validApiKey)
            return BadRequest("Invalid or expired API Key.");

        var urls = await _repository.GetAllUrlsFromUser(apiKey, limit);

        return Ok(urls);
    }
}
