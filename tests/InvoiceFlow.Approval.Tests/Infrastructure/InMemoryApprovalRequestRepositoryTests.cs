using InvoiceFlow.Approval.Domain;
using InvoiceFlow.Approval.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Approval.Tests.Infrastructure;

public sealed class InMemoryApprovalRequestRepositoryTests
{
    [Fact]
    public void TryAdd_ThenGetById_ReturnsSameRequest()
    {
        InMemoryApprovalRequestRepository repository = new();

        ApprovalRequest request = CreateRequest();

        bool added = repository.TryAdd(request);
        ApprovalRequest? retrieved = repository.GetById(request.Id);

        Assert.True(added);
        Assert.Same(request, retrieved);
    }

    [Fact]
    public void TryAdd_WithDuplicateInvoiceId_ReturnsFalse()
    {
        InMemoryApprovalRequestRepository repository = new();

        Guid invoiceId = Guid.NewGuid();

        ApprovalRequest firstRequest = CreateRequest(invoiceId);
        ApprovalRequest secondRequest = CreateRequest(invoiceId);

        Assert.True(repository.TryAdd(firstRequest));
        Assert.False(repository.TryAdd(secondRequest));
    }

    private static ApprovalRequest CreateRequest(Guid? invoiceId = null)
    {
        return ApprovalRequest.Create(
            invoiceId ?? Guid.NewGuid(),
            Money.Inr(50000m),
            1,
            DateTime.UtcNow);
    }
}
