using InvoiceFlow.Api.ErrorHandling;
using InvoiceFlow.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace InvoiceFlow.Api.Tests.ErrorHandling;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WhenUnexpectedException_ReturnsSafe500Response()
    {
        using ServiceProvider provider = CreateServiceProvider();
        using var responseBody = new MemoryStream();

        DefaultHttpContext context = CreateContext(provider, responseBody);
        IExceptionHandler handler = provider.GetRequiredService<IExceptionHandler>();

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("secret detail"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        string body = await ReadResponseBodyAsync(responseBody);

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(
            "test-correlation-id",
            root.GetProperty("correlationId").GetString());

        Assert.DoesNotContain("secret detail", body);
        Assert.Equal(
            "An unexpected error occurred.",
            root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_WhenBadHttpRequest_Returns400Response()
    {
        using ServiceProvider provider = CreateServiceProvider();
        using var responseBody = new MemoryStream();

        DefaultHttpContext context = CreateContext(provider, responseBody);
        IExceptionHandler handler = provider.GetRequiredService<IExceptionHandler>();

        bool handled = await handler.TryHandleAsync(
            context,
            new BadHttpRequestException(
                "Invalid request body",
                StatusCodes.Status400BadRequest),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        string body = await ReadResponseBodyAsync(responseBody);

        using JsonDocument document = JsonDocument.Parse(body);

        Assert.Equal(
            "Invalid request body",
            document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionOccurs_ReturnsProblemJsonContentType()
    {
        using ServiceProvider provider = CreateServiceProvider();
        using var responseBody = new MemoryStream();

        DefaultHttpContext context = CreateContext(provider, responseBody);
        IExceptionHandler handler = provider.GetRequiredService<IExceptionHandler>();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("test exception"),
            CancellationToken.None);

        Assert.StartsWith(
            "application/problem+json",
            context.Response.ContentType);
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionOccurs_RestoresCorrelationIdHeader()
    {
        using ServiceProvider provider = CreateServiceProvider();
        using var responseBody = new MemoryStream();

        DefaultHttpContext context = CreateContext(provider, responseBody);
        IExceptionHandler handler = provider.GetRequiredService<IExceptionHandler>();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("test exception"),
            CancellationToken.None);

        Assert.Equal(
            context.TraceIdentifier,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApiErrorHandling();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(
        IServiceProvider provider,
        MemoryStream responseBody)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "test-correlation-id"
        };

        context.Response.Body = responseBody;

        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Position = 0;

        using var reader = new StreamReader(responseBody, leaveOpen: true);

        return await reader.ReadToEndAsync();
    }
}
