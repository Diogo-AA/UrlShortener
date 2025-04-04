using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Options;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/url")]
[Authorize(Policy = ApiKeyAuthenticationOptions.DefaultPolicy)]
public class UrlController : ControllerBase
{
    private readonly IShortenerService _service;

    public UrlController(IShortenerService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] Uri url)
    {
        bool validUrl = Models.Url.IsValidUrl(url);
        if (!validUrl)
            return BadRequest("Invalid url format.");

        Guid apiKey = GetApiKeyFromClaims();

        string? shortedUrl = await _service.CreateShortedUrlAsync(apiKey, url);
        if (string.IsNullOrEmpty(shortedUrl))
            return BadRequest($"The url '{url}' is already shortened.");

        string uri = $"{Request.Scheme}:// {Request.Host}{Request.Path}";
        return Created(uri, shortedUrl);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromBody] string shortedUrlId)
    {
        Guid apiKey = GetApiKeyFromClaims();

        bool removed = await _service.RemoveUrlAsync(apiKey, shortedUrlId);
        if (!removed)
            return BadRequest("Shorted url id not found.");

        return NoContent();
    }

    [HttpGet("get/{shortedUrlId}")]
    public async Task<IActionResult> Get(string shortedUrlId)
    {
        Guid apiKey = GetApiKeyFromClaims();

        var url = await _service.GetUrlAsync(apiKey, shortedUrlId);
        if (url is null)
            return BadRequest("Url not found.");

        return Ok(url);
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] int limit = IShortenerService.DEFAULT_URLS_SHOWN)
    {
        if (limit < 0 || limit > IShortenerService.LIMIT_URLS_SHOWN)
            return BadRequest($"The limit of the results shown must be between 0 and {IShortenerService.LIMIT_URLS_SHOWN}");

        Guid apiKey = GetApiKeyFromClaims();

        var urls = await _service.GetUrlsAsync(apiKey, limit);

        return Ok(urls);
    }

    private Guid GetApiKeyFromClaims()
    {
        var claim = User.Claims.Where(claim => claim.Type == ApiKeyAuthenticationOptions.HeaderName).FirstOrDefault();
        if (!Guid.TryParse(claim?.Value, out Guid apiKey))
            throw new AuthenticationFailureException("Error getting the API key");
        return apiKey;
    }
}
