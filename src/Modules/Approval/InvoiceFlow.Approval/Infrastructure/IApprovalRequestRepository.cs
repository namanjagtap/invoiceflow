using InvoiceFlow.Approval.Domain;

namespace InvoiceFlow.Approval.Infrastructure;

internal interface IApprovalRequestRepository
{
    bool TryAdd(ApprovalRequest request);
    ApprovalRequest? GetById(Guid id);
}
