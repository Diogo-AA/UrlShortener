using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using UrlShortener.Infrastructure;

namespace UrlShortener.Handlers;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;
    private readonly IRepository _repository;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger, IRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
        string endpoint = exceptionHandlerFeature?.Path ?? httpContext.Request.Path;
        
        string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        await _repository.LogError(traceId, endpoint, exception);
        _logger.LogError("Error occurred in {Endpoint} with traceId {TraceId} at {Time}", endpoint, traceId, DateTime.UtcNow);

        return false;
    }
}