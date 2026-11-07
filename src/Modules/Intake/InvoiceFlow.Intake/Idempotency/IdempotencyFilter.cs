using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Idempotency;

internal sealed class IdempotencyFilter<TRequest>(
    IIdempotencyStore store) : IEndpointFilter
    where TRequest : class
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotent-Replayed";
    public const int MaxKeyLength = 64;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        HttpContext httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return await next(context);
        }

        string? key = values.Count == 1 ? values[0] : null;

        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid idempotency key",
                detail: $"The {HeaderName} header must be a single value of 1 to {MaxKeyLength} characters.");
        }

        TRequest? request = context.Arguments
            .OfType<TRequest>()
            .FirstOrDefault();

        string? userId = httpContext.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (request is null || userId is null)
        {
            return await next(context);
        }

        IdempotencyStart start = store.TryStart(
            userId,
            key,
            request);

        switch (start.Outcome)
        {
            case IdempotencyOutcome.Replay:
                httpContext.Response.Headers[ReplayedHeaderName] = "true";
                return start.Result!;

            case IdempotencyOutcome.InProgress:
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Request in progress",
                    detail: "A request with this idempotency key is already being processed. Please retry after it completes.");

            case IdempotencyOutcome.PayloadMismatch:
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Idempotency key reused",
                    detail: "This idempotency key has already been used with a different request payload. Use a new key for a different operation.");

            case IdempotencyOutcome.Started:
                break;

            default:
                throw new InvalidOperationException(
                    $"Unexpected idempotency outcome: {start.Outcome}");
        }

        object? result;

        try
        {
            result = await next(context);
        }
        catch
        {
            store.Release(userId, key);
            throw;
        }

        if (result is IResult httpResult && IsSuccess(httpResult))
        {
            store.Complete(userId, key, httpResult);
        }
        else
        {
            store.Release(userId, key);
        }

        return result;
    }

    private static bool IsSuccess(IResult result)
    {
        IResult inner = result is INestedHttpResult nested &&
            nested.Result is IResult nestedResult
            ? nestedResult
            : result;

        return inner is IStatusCodeHttpResult
        {
            StatusCode: >= 200 and < 300
        };
    }
}
