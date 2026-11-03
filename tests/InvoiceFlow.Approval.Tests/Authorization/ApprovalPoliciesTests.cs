using InvoiceFlow.Approval.Authorization;
using InvoiceFlow.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InvoiceFlow.Approval.Tests.Authorization;

public sealed class ApprovalPoliciesTests
{
    [Theory]
    [InlineData(AppRoles.ApClerk, ApprovalPolicies.CreateRequests, true)]
    [InlineData(AppRoles.Approver, ApprovalPolicies.CreateRequests, false)]
    [InlineData(AppRoles.Admin, ApprovalPolicies.CreateRequests, false)]
    [InlineData(AppRoles.Vendor, ApprovalPolicies.CreateRequests, false)]

    [InlineData(AppRoles.ApClerk, ApprovalPolicies.ReadRequests, true)]
    [InlineData(AppRoles.Approver, ApprovalPolicies.ReadRequests, true)]
    [InlineData(AppRoles.Admin, ApprovalPolicies.ReadRequests, true)]
    [InlineData(AppRoles.Vendor, ApprovalPolicies.ReadRequests, false)]

    [InlineData(AppRoles.ApClerk, ApprovalPolicies.Approve, false)]
    [InlineData(AppRoles.Approver, ApprovalPolicies.Approve, true)]
    [InlineData(AppRoles.Admin, ApprovalPolicies.Approve, false)]
    [InlineData(AppRoles.Vendor, ApprovalPolicies.Approve, false)]
    public async Task Authorize_WithRole_ReturnsExpectedResult(
        string role,
        string policy,
        bool expected)
    {
        using ServiceProvider provider = CreateProvider();

        IAuthorizationService authorizationService =
            provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = CreateUser(role);

        AuthorizationResult result =
            await authorizationService.AuthorizeAsync(user, policy);

        Assert.Equal(expected, result.Succeeded);
    }

    [Theory]
    [InlineData(ApprovalPolicies.CreateRequests)]
    [InlineData(ApprovalPolicies.ReadRequests)]
    [InlineData(ApprovalPolicies.Approve)]
    public async Task Authorize_WithoutRole_ReturnsFailure(string policy)
    {
        using ServiceProvider provider = CreateProvider();

        IAuthorizationService authorizationService =
            provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = new(
            new ClaimsIdentity([], "Test"));

        AuthorizationResult result =
            await authorizationService.AuthorizeAsync(user, policy);

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

    private static ClaimsPrincipal CreateUser(string role)
    {
        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.Role, role)
        ],
        "Test");

        return new ClaimsPrincipal(identity);
    }
}
