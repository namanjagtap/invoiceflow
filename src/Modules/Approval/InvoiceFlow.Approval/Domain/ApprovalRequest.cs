using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Approval.Domain;

internal sealed class ApprovalRequest
{
    private readonly List<ApprovalDecision> _decisions = [];

    private ApprovalRequest(
        Guid id,
        Guid invoiceId,
        Money amount,
        int requiredApprovals,
        DateTime createdAtUtc)
    {
        Id = id;
        InvoiceId = invoiceId;
        Amount = amount;
        RequiredApprovals = requiredApprovals;
        Status = ApprovalStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; }
    public int RequiredApprovals { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<ApprovalDecision> Decisions => _decisions.AsReadOnly();

    public static ApprovalRequest Create(
        Guid invoiceId,
        Money amount,
        int requiredApprovals,
        DateTime utcNow)
    {
        if (invoiceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invoice ID cannot be empty.",
                nameof(invoiceId));
        }

        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Approval amount must be greater than zero.");
        }

        if (requiredApprovals < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredApprovals),
                "At least one approval is required.");
        }

        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The timestamp must be UTC.",
                nameof(utcNow));
        }

        return new ApprovalRequest(
            Guid.CreateVersion7(),
            invoiceId,
            amount,
            requiredApprovals,
            utcNow);
    }

    public bool HasApproved(string approverId)
    {
        return _decisions.Any(
            decision => string.Equals(
                decision.ApproverId,
                approverId,
                StringComparison.Ordinal));
    }

    public void Approve(string approverId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(approverId))
        {
            throw new ArgumentException(
                "Approver ID cannot be null or whitespace.",
                nameof(approverId));
        }

        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The timestamp must be UTC.",
                nameof(utcNow));
        }

        if (Status == ApprovalStatus.Approved)
        {
            throw new InvalidOperationException(
                "This approval request has already been approved.");
        }

        if (HasApproved(approverId))
        {
            throw new InvalidOperationException(
                "This approver has already approved this request.");
        }

        _decisions.Add(new ApprovalDecision(approverId, utcNow));

        if (_decisions.Count >= RequiredApprovals)
        {
            Status = ApprovalStatus.Approved;
        }
    }
}
