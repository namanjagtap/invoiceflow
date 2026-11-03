using Microsoft.AspNetCore.Authorization;

namespace InvoiceFlow.Approval.Authorization;

internal sealed class ApprovalLimitRequirement : IAuthorizationRequirement
{
}
