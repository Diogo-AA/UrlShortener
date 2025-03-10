using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;
using UrlShortener.Models;

namespace UrlShortener.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DbController _dbController;

        public ApiController(DbController dbController)
        {
            _dbController = dbController;
        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] User userRequest, [FromQuery] bool updateApiKeyIfExpired)
        {
            bool validCredentials = await _dbController.VerifyUser(userRequest);
            if (!validCredentials)
                return BadRequest("Username or password incorrect.");

            Guid? apiKey = await _dbController.GetApiKey(userRequest.Id);
            if (!apiKey.HasValue && !updateApiKeyIfExpired)
            {
                return Ok("API Key expired. Update your API Key using the 'update' endpoint or defining the parameter 'updateApiKeyIfExpired' to true.");
            }
            else if (!apiKey.HasValue)
            {
                return await UpdateApiKey(userRequest.Id);
            }

            return Ok(apiKey.Value);
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] User userRequest)
        {
            bool validCredentials = await _dbController.VerifyUser(userRequest);
            if (!validCredentials)
                return BadRequest("Username or password incorrect.");

            DateTime? lastUpdated = await _dbController.GetLastTimeApiKeyUpdated(userRequest.Id);
            if (!lastUpdated.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            if (DateTime.Now.Subtract(lastUpdated.Value).Minutes <= ApiKey.MAX_MINUTES_BETWEEN_UPDATES)
                return StatusCode(StatusCodes.Status429TooManyRequests, "API Key changed recently. Wait a few minutes before updating again.");

            return await UpdateApiKey(userRequest.Id);
        }

        private async Task<IActionResult> UpdateApiKey(Guid userId)
        {
            Guid? apiKey = await _dbController.UpdateApiKey(userId);
            if (!apiKey.HasValue)
                return Problem("Error updating the API Key. Try again later.");

            return Ok(apiKey.Value);
        }
    }
}
