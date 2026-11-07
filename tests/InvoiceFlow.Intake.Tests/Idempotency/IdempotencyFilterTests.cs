using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Tests.Idempotency;

public sealed class IdempotencyFilterTests
{
    [Fact]
    public async Task InvokeAsync_WhenHeaderIsMissing_PassesThrough()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;
            return ValueTask.FromResult<object?>(
                TypedResults.Ok("Success"));
        };

        DefaultHttpContext httpContext = CreateHttpContext("clerk1");
        SubmitInvoiceRequest request = CreateRequest();

        DefaultEndpointFilterInvocationContext context = new(
            httpContext,
            request);

        await filter.InvokeAsync(context, next);
        await filter.InvokeAsync(context, next);

        Assert.Equal(2, nextCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(
        "12345678901234567890123456789012345678901234567890123456789012345")]
    public async Task InvokeAsync_WhenKeyIsInvalid_ReturnsBadRequest(
        string key)
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;
            return ValueTask.FromResult<object?>(TypedResults.Ok());
        };

        DefaultHttpContext httpContext = CreateHttpContext("clerk1");
        httpContext.Request.Headers[IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = key;

        DefaultEndpointFilterInvocationContext context = new(
            httpContext,
            CreateRequest());

        object? result = await filter.InvokeAsync(context, next);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Invalid idempotency key", problem.ProblemDetails.Title);
        Assert.Equal(0, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyIsReusedWithSameRequest_ReplaysResult()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;
        SubmitInvoiceRequest request = CreateRequest();

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;
            return ValueTask.FromResult<object?>(
                TypedResults.Ok("Invoice created"));
        };

        DefaultHttpContext firstHttpContext = CreateHttpContext("clerk1");
        firstHttpContext.Request.Headers[
            IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

        DefaultEndpointFilterInvocationContext firstContext = new(
            firstHttpContext,
            request);

        object? originalResult = await filter.InvokeAsync(
            firstContext,
            next);

        DefaultHttpContext retryHttpContext = CreateHttpContext("clerk1");
        retryHttpContext.Request.Headers[
            IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

        DefaultEndpointFilterInvocationContext retryContext = new(
            retryHttpContext,
            request);

        object? replayedResult = await filter.InvokeAsync(
            retryContext,
            next);

        Assert.Same(originalResult, replayedResult);
        Assert.Equal(1, nextCallCount);

        Assert.Equal(
            "true",
            retryHttpContext.Response.Headers[
                IdempotencyFilter<SubmitInvoiceRequest>.ReplayedHeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsFailure_DoesNotStoreResult()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;
        SubmitInvoiceRequest request = CreateRequest();

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;

            return ValueTask.FromResult<object?>(
                TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Duplicate invoice"));
        };

        for (int i = 0; i < 2; i++)
        {
            DefaultHttpContext httpContext = CreateHttpContext("clerk1");
            httpContext.Request.Headers[
                IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

            DefaultEndpointFilterInvocationContext context = new(
                httpContext,
                request);

            await filter.InvokeAsync(context, next);
        }

        Assert.Equal(2, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenSameKeyHasDifferentRequest_ReturnsPayloadMismatch()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;

            return ValueTask.FromResult<object?>(
                TypedResults.Ok("Invoice created"));
        };

        DefaultHttpContext firstHttpContext = CreateHttpContext("clerk1");
        firstHttpContext.Request.Headers[
            IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

        DefaultEndpointFilterInvocationContext firstContext = new(
            firstHttpContext,
            CreateRequest("INV-001"));

        await filter.InvokeAsync(firstContext, next);

        DefaultHttpContext secondHttpContext = CreateHttpContext("clerk1");
        secondHttpContext.Request.Headers[
            IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

        DefaultEndpointFilterInvocationContext secondContext = new(
            secondHttpContext,
            CreateRequest("INV-002"));

        object? result = await filter.InvokeAsync(secondContext, next);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(result);

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            problem.StatusCode);

        Assert.Equal("Idempotency key reused", problem.ProblemDetails.Title);
        Assert.Equal(1, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenSameKeyIsUsedByDifferentUsers_BothRequestsPassThrough()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;
        SubmitInvoiceRequest request = CreateRequest();

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;

            return ValueTask.FromResult<object?>(
                TypedResults.Ok("Invoice created"));
        };

        foreach (string userId in new[] { "clerk1", "clerk2" })
        {
            DefaultHttpContext httpContext = CreateHttpContext(userId);
            httpContext.Request.Headers[
                IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] = "key-1";

            DefaultEndpointFilterInvocationContext context = new(
                httpContext,
                request);

            await filter.InvokeAsync(context, next);
        }

        Assert.Equal(2, nextCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenMultipleKeysAreProvided_ReturnsBadRequest()
    {
        InMemoryIdempotencyStore store = new(TimeProvider.System);
        IdempotencyFilter<SubmitInvoiceRequest> filter = new(store);

        int nextCallCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            nextCallCount++;
            return ValueTask.FromResult<object?>(TypedResults.Ok());
        };

        DefaultHttpContext httpContext = CreateHttpContext("clerk1");

        httpContext.Request.Headers[
            IdempotencyFilter<SubmitInvoiceRequest>.HeaderName] =
            new StringValues(["key-1", "key-2"]);

        DefaultEndpointFilterInvocationContext context = new(
            httpContext,
            CreateRequest());

        object? result = await filter.InvokeAsync(context, next);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(0, nextCallCount);
    }

    private static DefaultHttpContext CreateHttpContext(string userId)
    {
        DefaultHttpContext context = new();

        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.NameIdentifier, userId)
        ],
        "Test");

        context.User = new ClaimsPrincipal(identity);

        return context;
    }

    private static SubmitInvoiceRequest CreateRequest(
        string invoiceNumber = "INV-001")
    {
        return new SubmitInvoiceRequest(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            invoiceNumber,
            new DateOnly(2026, 5, 10),
            null,
            1000m,
            180m,
            1180m,
            Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }
}
