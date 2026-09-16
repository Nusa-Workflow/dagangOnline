using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace dagangOnline.Infrastructure.Logging;

public class GrpcObservabilityInterceptor : Interceptor
{
    private readonly ILogger<GrpcObservabilityInterceptor> _logger;

    public GrpcObservabilityInterceptor(ILogger<GrpcObservabilityInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, 
        ServerCallContext context, 
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = context.RequestHeaders.FirstOrDefault(h => h.Key.Equals("x-correlation-id", StringComparison.OrdinalIgnoreCase))?.Value 
                            ?? Guid.NewGuid().ToString();

        // Add to response trailers so client gets it back
        context.ResponseTrailers.Add("x-correlation-id", correlationId);

        var method = context.Method;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await continuation(request, context);
            sw.Stop();
            
            _logger.LogInformation("gRPC Request Completed: {Method} responded {StatusCode} in {ElapsedMilliseconds}ms. CorrelationId: {CorrelationId}", 
                method, context.Status.StatusCode, sw.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (RpcException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "gRPC Request Error: {Method} responded {StatusCode} in {ElapsedMilliseconds}ms. CorrelationId: {CorrelationId}", 
                method, ex.StatusCode, sw.ElapsedMilliseconds, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Translate to generic gRPC Internal error to prevent leaking stack traces
            _logger.LogError(ex, "gRPC Request Unhandled Exception: {Method} in {ElapsedMilliseconds}ms. CorrelationId: {CorrelationId}", 
                method, sw.ElapsedMilliseconds, correlationId);
                
            throw new RpcException(new Status(StatusCode.Internal, "An internal error occurred. Reference ID: " + correlationId));
        }
    }
}
