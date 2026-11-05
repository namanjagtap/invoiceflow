using InvoiceFlow.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Intake.Authorization;

internal static class IntakePolicies
{
    public const string ManageVendors = "Intake.ManageVendors";
    public const string ReadVendors = "Intake.ReadVendors";
    public const string SubmitInvoices = "Intake.SubmitInvoices";
    public const string ReadInvoices = "Intake.ReadInvoices";

    internal static IServiceCollection AddIntakePolicies(
        this IServiceCollection services)
    {
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(
                ManageVendors,
                policy => policy.RequireRole(AppRoles.Admin));
            options.AddPolicy(
                ReadVendors,
                policy => policy.RequireRole(
                    AppRoles.ApClerk,
                    AppRoles.Approver,
                    AppRoles.Admin));
            options.AddPolicy(
                SubmitInvoices,
                policy => policy.RequireRole(
                    AppRoles.Vendor,
                    AppRoles.ApClerk));
            options.AddPolicy(
                ReadInvoices,
                policy => policy.RequireRole(
                    AppRoles.Vendor,
                    AppRoles.ApClerk,
                    AppRoles.Approver,
                    AppRoles.Admin));
        });

        return services;
    }
}
