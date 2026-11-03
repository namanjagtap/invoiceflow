namespace InvoiceFlow.Approval.Domain;

internal sealed record ApprovalDecision(
    string ApproverId,
    DateTime ApprovedAtUtc);
