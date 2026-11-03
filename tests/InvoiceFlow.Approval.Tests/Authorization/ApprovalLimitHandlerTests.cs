using InvoiceFlow.Approval.Authorization;
using InvoiceFlow.Approval.Domain;
using InvoiceFlow.SharedKernel.Security;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InvoiceFlow.Approval.Tests.Authorization;

public sealed class ApprovalLimitHandlerTests
{
    [Theory]
    [InlineData("100000", 50000, true)]
    [InlineData("100000", 100000, true)]
    [InlineData("100000", 100000.01, false)]
    [InlineData("100000.50", 100000.50, true)]
    [InlineData("abc", 50000, false)]
    [InlineData("1,00,000", 50000, false)]
    [InlineData("-1", 50000, false)]
    public async Task Authorize_WithApprovalLimitClaim_ReturnsExpectedResult(
        string approvalLimit,
        decimal invoiceAmount,
        bool expected)
    {
        using ServiceProvider provider = CreateProvider();

        IAuthorizationService authorizationService =
            provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = CreateUser(approvalLimit);
        ApprovalRequest request = CreateRequest(invoiceAmount);

        AuthorizationResult result =
            await authorizationService.AuthorizeAsync(
                user,
                request,
                ApprovalPolicies.WithinApprovalLimit);

        Assert.Equal(expected, result.Succeeded);
    }

    [Fact]
    public async Task Authorize_WithoutApprovalLimitClaim_ReturnsFailure()
    {
        using ServiceProvider provider = CreateProvider();

        IAuthorizationService authorizationService =
            provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = CreateUserWithoutApprovalLimit();
        ApprovalRequest request = CreateRequest(50000m);

        AuthorizationResult result =
            await authorizationService.AuthorizeAsync(
                user,
                request,
                ApprovalPolicies.WithinApprovalLimit);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddApprovalPolicies();

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static ClaimsPrincipal CreateUser(string approvalLimit)
    {
        ClaimsIdentity identity = new(
        [
            new Claim(AppClaims.ApprovalLimit, approvalLimit)
        ],
        "Test");

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUserWithoutApprovalLimit()
    {
        ClaimsIdentity identity = new([], "Test");

        return new ClaimsPrincipal(identity);
    }

    private static ApprovalRequest CreateRequest(decimal amount)
    {
        return ApprovalRequest.Create(
            Guid.NewGuid(),
            Money.Inr(amount),
            1,
            DateTime.UtcNow);
    }
}
