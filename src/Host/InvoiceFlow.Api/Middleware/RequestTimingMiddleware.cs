using System.Diagnostics;

namespace InvoiceFlow.Api.Middleware;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public const long SlowRequestThresholdMs = 500;

    public async Task InvokeAsync(HttpContext context)
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

            LogLevel level = elapsedMs > SlowRequestThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            logger.Log(
                level,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
        }
    }
}
