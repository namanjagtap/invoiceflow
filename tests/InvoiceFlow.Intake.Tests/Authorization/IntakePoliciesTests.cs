using InvoiceFlow.Intake.Authorization;
using InvoiceFlow.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Tests.Authorization;

public sealed class IntakePoliciesTests
{
    public static TheoryData<string, string, bool> RolePolicyCases => new()
    {
        // ManageVendors
        { IntakePolicies.ManageVendors, AppRoles.Vendor, false },
        { IntakePolicies.ManageVendors, AppRoles.ApClerk, false },
        { IntakePolicies.ManageVendors, AppRoles.Approver, false },
        { IntakePolicies.ManageVendors, AppRoles.Admin, true },

        // ReadVendors
        { IntakePolicies.ReadVendors, AppRoles.Vendor, false },
        { IntakePolicies.ReadVendors, AppRoles.ApClerk, true },
        { IntakePolicies.ReadVendors, AppRoles.Approver, true },
        { IntakePolicies.ReadVendors, AppRoles.Admin, true },

        // SubmitInvoices
        { IntakePolicies.SubmitInvoices, AppRoles.Vendor, true },
        { IntakePolicies.SubmitInvoices, AppRoles.ApClerk, true },
        { IntakePolicies.SubmitInvoices, AppRoles.Approver, false },
        { IntakePolicies.SubmitInvoices, AppRoles.Admin, false },

        // ReadInvoices
        { IntakePolicies.ReadInvoices, AppRoles.Vendor, false },
        { IntakePolicies.ReadInvoices, AppRoles.ApClerk, true },
        { IntakePolicies.ReadInvoices, AppRoles.Approver, true },
        { IntakePolicies.ReadInvoices, AppRoles.Admin, true }
    };

    public static TheoryData<string> PolicyNames => new()
    {
        IntakePolicies.ManageVendors,
        IntakePolicies.ReadVendors,
        IntakePolicies.SubmitInvoices,
        IntakePolicies.ReadInvoices
    };

    [Theory]
    [MemberData(nameof(RolePolicyCases))]
    public async Task AuthorizeAsync_RoleAgainstPolicy_ReturnsExpectedResult(
        string policyName,
        string role,
        bool expected)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider();

        IAuthorizationService authorization =
            serviceProvider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = CreateUser(role);

        AuthorizationResult result =
            await authorization.AuthorizeAsync(user, policyName);

        Assert.Equal(expected, result.Succeeded);
    }

    [Theory]
    [MemberData(nameof(PolicyNames))]
    public async Task AuthorizeAsync_UserWithoutRole_FailsPolicy(
        string policyName)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider();

        IAuthorizationService authorization =
            serviceProvider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal user = new(
            new ClaimsIdentity([], "Test"));

        AuthorizationResult result =
            await authorization.AuthorizeAsync(user, policyName);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddIntakePolicies();

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreateUser(string role)
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.Role, role)],
            "Test");

        return new ClaimsPrincipal(identity);
    }
}
