using InvoiceFlow.SharedKernel.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace InvoiceFlow.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SubmissionRateLimitOptions>()
            .Bind(configuration.GetSection(
                SubmissionRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds)
                        .ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };

            options.AddPolicy(
                RateLimitPolicies.Submissions,
                httpContext =>
                {
                    SubmissionRateLimitOptions rateLimitOptions =
                        httpContext.RequestServices.GetRequiredService<IOptions<SubmissionRateLimitOptions>>().Value;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        GetPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.PermitLimit,
                            Window = TimeSpan.FromSeconds(
                                rateLimitOptions.WindowSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        });
                });
        });
        return services;
    }

    public static string GetPartitionKey(HttpContext httpContext)
    {
        string? userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is not null)
        {
            return $"user:{userId}";
        }

        string ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return $"ip:{ipAddress}";
    }
}
