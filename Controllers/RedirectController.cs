using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortener.Data;
using UrlShortener.Options;

namespace UrlShortener.Controllers
{
    [Route("")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly IDistributedCache _cache;

        public RedirectController(IRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        [HttpGet("{shortedUrlId:length(6)}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(string shortedUrlId)
        {
            string? url = await _cache.GetStringAsync(shortedUrlId);
            Console.WriteLine($"URL obtained from cache: {url}");

            if (string.IsNullOrEmpty(url))
                url = await _repository.GetOriginalUrl(shortedUrlId);

            if (string.IsNullOrEmpty(url))
                return NotFound("Page not found.");

            await _cache.SetStringAsync(shortedUrlId, url, CacheOptions.Options);

            return Redirect(url);
        }
    }
}
