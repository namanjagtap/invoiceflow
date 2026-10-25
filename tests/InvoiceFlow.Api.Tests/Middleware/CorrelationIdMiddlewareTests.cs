using InvoiceFlow.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceFlow.Api.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    public static TheoryData<string> InvalidIds => new()
    {
        "abc\nFAKE LOG LINE",
        "abc<script>",
        new string('a', 65),
        ""
    };

    private static CorrelationIdMiddleware CreateMiddleware(
        RequestDelegate? next = null)
    {
        return new CorrelationIdMiddleware(
            next ?? (_ => Task.CompletedTask),
            NullLogger<CorrelationIdMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_NoIncomingId_GeneratesIdAndCallsNext()
    {
        bool nextCalled = false;

        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        string id = context.Response.Headers[
            CorrelationIdMiddleware.HeaderName].ToString();

        Assert.True(nextCalled);
        Assert.Equal(id, context.TraceIdentifier);
        Assert.Matches("^[0-9a-fA-F]{32}$", id);
    }

    [Fact]
    public async Task InvokeAsync_ValidIncomingId_ReusesSameId()
    {
        const string incomingId = "my-test-123";

        var context = new DefaultHttpContext();
        context.Request.Headers[
            CorrelationIdMiddleware.HeaderName] = incomingId;

        await CreateMiddleware().InvokeAsync(context);

        string responseId = context.Response.Headers[
            CorrelationIdMiddleware.HeaderName].ToString();

        Assert.Equal(incomingId, responseId);
        Assert.Equal(incomingId, context.TraceIdentifier);
    }

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public async Task InvokeAsync_InvalidIncomingId_ReplacesIt(
        string invalidId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[
            CorrelationIdMiddleware.HeaderName] = invalidId;

        await CreateMiddleware().InvokeAsync(context);

        string responseId = context.Response.Headers[
            CorrelationIdMiddleware.HeaderName].ToString();

        Assert.NotEqual(invalidId, responseId);
        Assert.Equal(responseId, context.TraceIdentifier);
        Assert.Matches("^[0-9a-fA-F]{32}$", responseId);
    }

    [Fact]
    public async Task InvokeAsync_64CharacterIncomingId_ReusesSameId()
    {
        string incomingId = new('a', 64);

        var context = new DefaultHttpContext();
        context.Request.Headers[
            CorrelationIdMiddleware.HeaderName] = incomingId;

        await CreateMiddleware().InvokeAsync(context);

        string responseId = context.Response.Headers[
            CorrelationIdMiddleware.HeaderName].ToString();

        Assert.Equal(incomingId, responseId);
        Assert.Equal(incomingId, context.TraceIdentifier);
    }
}
