using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortener.Options;

namespace UrlShortener.Controllers
{
    [Route("api/api-key")]
    [ApiController]
    [Authorize(Policy = UserAndPasswordAuthenticationOptions.DefaultPolicy)]
    public class ApiKeyController : ControllerBase
    {
        private readonly IRepository _repository;

        public ApiKeyController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("get")]
        public async Task<IActionResult> GetApiKey([FromQuery] bool? updateIfExpired)
        {
            var userId = GetUserIdFromClaims();

            Guid? apiKey = await _repository.GetApiKey(userId);
            if (!apiKey.HasValue && !updateIfExpired.GetValueOrDefault())
            {
                return Ok("API Key expired. Update your API Key using the 'update' endpoint or defining the parameter 'updateIfExpired' to true.");
            }
            else if (!apiKey.HasValue)
            {
                return await UpdateApiKey(userId);
            }

            return Ok(apiKey.Value);
        }

        [HttpPatch("update")]
        public async Task<IActionResult> Update()
        {
            var userId = GetUserIdFromClaims();

            DateTime? lastUpdated = await _repository.GetLastTimeApiKeyUpdated(userId);
            if (!lastUpdated.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            if (DateTime.Now.Subtract(lastUpdated.Value).Minutes <= ApiKey.MAX_MINUTES_BETWEEN_UPDATES)
                return StatusCode(StatusCodes.Status429TooManyRequests, "API Key changed recently. Wait a few minutes before updating again.");

            return await UpdateApiKey(userId);
        }

        private async Task<IActionResult> UpdateApiKey(Guid userId)
        {
            Guid? apiKey = await _repository.UpdateApiKey(userId);
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
