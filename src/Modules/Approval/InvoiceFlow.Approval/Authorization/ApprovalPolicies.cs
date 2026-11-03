using InvoiceFlow.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Approval.Authorization;

internal static class ApprovalPolicies
{
    public const string CreateRequests = "Approval.CreateRequests";
    public const string ReadRequests = "Approval.ReadRequests";
    public const string Approve = "Approval.Approve";
    public const string WithinApprovalLimit = "Approval.WithinApprovalLimit";

    internal static IServiceCollection AddApprovalPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ApprovalLimitHandler>();
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(
                CreateRequests,
                policy => policy.RequireRole(AppRoles.ApClerk));

            options.AddPolicy(
                ReadRequests,
                policy => policy.RequireRole(
                    AppRoles.ApClerk,
                    AppRoles.Approver,
                    AppRoles.Admin));

            options.AddPolicy(
                Approve,
                policy => policy.RequireRole(AppRoles.Approver));

            options.AddPolicy(
                WithinApprovalLimit,
                policy => policy.AddRequirements(
                    new ApprovalLimitRequirement()));
        });

        return services;
    }
}
