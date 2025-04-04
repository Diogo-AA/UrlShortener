using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Options;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    [Route("api/api-key")]
    [ApiController]
    [Authorize(Policy = UserAndPasswordAuthenticationOptions.DefaultPolicy)]
    public class ApiKeyController : ControllerBase
    {
        private readonly IShortenerService _service;

        public ApiKeyController(IShortenerService service)
        {
            _service = service;
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetApiKey()
        {
            var userId = GetUserIdFromClaims();

            Guid? apiKey = await _service.GetApiKeyAsync(userId);
            if (!apiKey.HasValue)
            {
                return Problem(
                    title: "Expired API Key", 
                    detail: "Update your API Key using the 'update' endpoint",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }
            
            return Ok(apiKey.Value);
        }

        [HttpPatch("update")]
        public async Task<IActionResult> Update()
        {
            var userId = GetUserIdFromClaims();

            DateTime? lastUpdated = await _service.GetLastTimeApiKeyUpdatedAsync(userId);
            if (!lastUpdated.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            if (DateTime.Now.Subtract(lastUpdated.Value).Minutes <= ApiKey.MAX_MINUTES_BETWEEN_UPDATES)
                return StatusCode(StatusCodes.Status429TooManyRequests, "API Key changed recently. Wait a few minutes before updating again.");
            
            Guid? apiKey = await _service.UpdateApiKeyAsync(userId);
            if (!apiKey.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            return Ok(apiKey.Value);
        }

        private Guid GetUserIdFromClaims()
        {
            var claim = User.Claims.Where(claim => claim.Type == "userId").FirstOrDefault();
            if (!Guid.TryParse(claim?.Value, out Guid userId))
                throw new AuthenticationFailureException("Error getting the userId");
            return userId;
        }
    }
}
