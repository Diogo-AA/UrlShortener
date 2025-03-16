using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Infrastructure;

namespace UrlShortener.Controllers
{
    [Route("")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IRepository _repository;

        public RedirectController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{shortedUrlId:minlength(6)}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(string shortedUrlId)
        {
            string? url = await _repository.GetOriginalUrl(shortedUrlId);

            if (string.IsNullOrEmpty(url))
                return NotFound("Page not found.");

            return Redirect(url);
        }
    }
}
