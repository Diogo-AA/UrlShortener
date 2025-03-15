using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.Controllers
{
    [NonController]
    public class ErrorController : ControllerBase
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/error")]
        public IActionResult HandleError() => Problem();
    }
}
