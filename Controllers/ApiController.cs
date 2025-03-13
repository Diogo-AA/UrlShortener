using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;
using UrlShortener.Models;

namespace UrlShortener.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IRepository _repository;

        public ApiController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] User userRequest, [FromQuery] bool? updateIfExpired)
        {
            bool validCredentials = await _repository.VerifyUser(userRequest);
            if (!validCredentials)
                return BadRequest("Username or password incorrect.");

            Guid? apiKey = await _repository.GetApiKey(userRequest.Id);
            if (!apiKey.HasValue && !updateIfExpired.GetValueOrDefault())
            {
                return Ok("API Key expired. Update your API Key using the 'update' endpoint or defining the parameter 'updateIfExpired' to true.");
            }
            else if (!apiKey.HasValue)
            {
                return await UpdateApiKey(userRequest.Id);
            }

            return Ok(apiKey.Value);
        }

        [HttpPatch("update")]
        public async Task<IActionResult> Update([FromBody] User userRequest)
        {
            bool validCredentials = await _repository.VerifyUser(userRequest);
            if (!validCredentials)
                return BadRequest("Username or password incorrect.");

            DateTime? lastUpdated = await _repository.GetLastTimeApiKeyUpdated(userRequest.Id);
            if (!lastUpdated.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            if (DateTime.Now.Subtract(lastUpdated.Value).Minutes <= ApiKey.MAX_MINUTES_BETWEEN_UPDATES)
                return StatusCode(StatusCodes.Status429TooManyRequests, "API Key changed recently. Wait a few minutes before updating again.");

            return await UpdateApiKey(userRequest.Id);
        }

        private async Task<IActionResult> UpdateApiKey(Guid userId)
        {
            Guid? apiKey = await _repository.UpdateApiKey(userId);
            if (!apiKey.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            return Ok(apiKey.Value);
        }
    }
}
