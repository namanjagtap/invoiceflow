using InvoiceFlow.Approval.Domain;
using InvoiceFlow.SharedKernel.Security;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace InvoiceFlow.Approval.Authorization;

internal sealed class ApprovalLimitHandler : AuthorizationHandler<ApprovalLimitRequirement, ApprovalRequest>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApprovalLimitRequirement requirement,
        ApprovalRequest resource)
    {
        string? approvalLimitClaim = context.User.FindFirst(AppClaims.ApprovalLimit)?.Value;

        bool hasValidLimit = decimal.TryParse(
            approvalLimitClaim,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out decimal approvalLimit);

        if (hasValidLimit && resource.Amount.Amount <= approvalLimit)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
