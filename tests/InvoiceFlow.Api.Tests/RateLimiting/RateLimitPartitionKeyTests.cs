using InvoiceFlow.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace InvoiceFlow.Api.Tests.RateLimiting;

public sealed class RateLimitPartitionKeyTests
{
    [Fact]
    public void GetPartitionKey_WhenUserHasNameIdentifier_ReturnsUserPartition()
    {
        DefaultHttpContext httpContext = new();

        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.NameIdentifier, "clerk1")
        ],
        "Test");

        httpContext.User = new ClaimsPrincipal(identity);
        httpContext.Connection.RemoteIpAddress =
            IPAddress.Parse("10.0.0.1");

        string result = RateLimitingExtensions.GetPartitionKey(httpContext);

        Assert.Equal("user:clerk1", result);
    }

    [Fact]
    public void GetPartitionKey_WhenUserIsUnauthenticated_ReturnsIpPartition()
    {
        DefaultHttpContext httpContext = new();

        httpContext.Connection.RemoteIpAddress =
            IPAddress.Parse("10.0.0.1");

        string result = RateLimitingExtensions.GetPartitionKey(httpContext);

        Assert.Equal("ip:10.0.0.1", result);
    }
}
