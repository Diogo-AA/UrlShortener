using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    [Route("")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IShortenerService _service;
        private readonly IDistributedCache _cache;

        public RedirectController(IShortenerService service, IDistributedCache cache)
        {
            _service = service;
            _cache = cache;
        }

        [HttpGet("{shortedUrlId:length(6)}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(string shortedUrlId)
        {
            string? url = await _service.GetOriginalUrlAsync(shortedUrlId);
            if (string.IsNullOrEmpty(url))
                return NotFound();

            return Redirect(url);
        }
    }
}
