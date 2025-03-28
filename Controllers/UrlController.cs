using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Data;
using UrlShortener.Options;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/url")]
[Authorize(Policy = ApiKeyAuthenticationOptions.DefaultPolicy)]
public class UrlController : ControllerBase
{
    private readonly IRepository _repository;

    public UrlController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] Uri url)
    {
        bool validUrl = Models.Url.IsValidUrl(url);
        if (!validUrl)
            return BadRequest("Invalid url format.");

        Guid apiKey = GetApiKeyFromClaims();

        string? shortedUrl = await _repository.CreateShortedUrl(apiKey, url);
        if (string.IsNullOrEmpty(shortedUrl))
            return BadRequest($"The url '{url}' is already shortened.");

        return Ok(shortedUrl);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromBody] string shortedUrlId)
    {
        Guid apiKey = GetApiKeyFromClaims();

        bool removed = await _repository.RemoveUrl(apiKey, shortedUrlId);
        if (!removed)
            return BadRequest("Shorted url id not found.");

        return Ok();
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] int limit = IRepository.DEFAULT_URLS_SHOWN)
    {
        if (limit < 0 || limit > IRepository.LIMIT_URLS_SHOWN)
            return BadRequest($"The limit of the results shown must be between 0 and {IRepository.LIMIT_URLS_SHOWN}");

        Guid apiKey = GetApiKeyFromClaims();

        var urls = await _repository.GetAllUrlsFromUser(apiKey, limit);

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
