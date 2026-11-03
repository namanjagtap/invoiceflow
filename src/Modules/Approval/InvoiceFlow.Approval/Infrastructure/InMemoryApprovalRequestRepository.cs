using InvoiceFlow.Approval.Domain;
using System.Collections.Concurrent;

namespace InvoiceFlow.Approval.Infrastructure;

internal sealed class InMemoryApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly ConcurrentDictionary<Guid, ApprovalRequest> _requestsById = new();
    private readonly ConcurrentDictionary<Guid, Guid> _requestIdByInvoiceId = new();

    public bool TryAdd(ApprovalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_requestIdByInvoiceId.TryAdd(
            request.InvoiceId,
            request.Id))
        {
            return false;
        }

        if (_requestsById.TryAdd(request.Id, request))
        {
            return true;
        }

        _requestIdByInvoiceId.TryRemove(request.InvoiceId, out _);

        return false;
    }

    public ApprovalRequest? GetById(Guid id)
    {
        return _requestsById.TryGetValue(id, out ApprovalRequest? request)
            ? request
            : null;
    }
}
