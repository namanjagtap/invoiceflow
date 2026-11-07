using System.ComponentModel.DataAnnotations;

namespace InvoiceFlow.Api.RateLimiting;

internal sealed class SubmissionRateLimitOptions
{
    public const string SectionName = "RateLimiting:Submissions";

    [Range(1, 1000)]
    public int PermitLimit { get; init; }

    [Range(1, 3600)]
    public int WindowSeconds { get; init; }
}
