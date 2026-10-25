using System.Diagnostics.CodeAnalysis;

namespace InvoiceFlow.Api.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string? incomingId = context.Request.Headers[HeaderName].FirstOrDefault();

        string id = IsValidCorrelationId(incomingId)
            ? incomingId
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = id;
        context.Response.Headers[HeaderName] = id;

        using (logger.BeginScope("CorrelationId: {CorrelationId}", id))
        {
            await next(context);
        }
    }

    private static bool IsValidCorrelationId([NotNullWhen(true)] string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64)
        {
            return false;
        }

        foreach (char character in id)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-')
            {
                return false;
            }
        }

        return true;
    }
}
