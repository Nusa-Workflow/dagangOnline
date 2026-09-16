using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace dagangOnline.Infrastructure.Logging;

public class ObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ObservabilityMiddleware> _logger;

    public ObservabilityMiddleware(RequestDelegate next, ILogger<ObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                            ?? Guid.NewGuid().ToString();
        
        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        // 2. Setup timing
        var sw = Stopwatch.StartNew();
        bool isError = false;

        try
        {
            // Proceed with request
            await _next(context);
        }
        catch (Exception ex)
        {
            isError = true;
            _logger.LogError(ex, "Unhandled exception processing request {Method} {Path}. CorrelationId: {CorrelationId}", 
                context.Request.Method, context.Request.Path, correlationId);
            
            // Re-throw to let global exception handler catch it
            throw;
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            
            if (statusCode >= 400)
                isError = true;

            // Safe structured logging - Do not log body/cookies/tokens
            var logMessage = "REST Request Completed: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms. CorrelationId: {CorrelationId}";

            if (isError)
            {
                _logger.LogWarning(logMessage, context.Request.Method, context.Request.Path, statusCode, sw.ElapsedMilliseconds, correlationId);
            }
            else
            {
                _logger.LogInformation(logMessage, context.Request.Method, context.Request.Path, statusCode, sw.ElapsedMilliseconds, correlationId);
            }
        }
    }
}
