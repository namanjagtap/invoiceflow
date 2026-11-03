namespace InvoiceFlow.Approval.Endpoints;

internal sealed record ApprovalRequestResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Status,
    int RequiredApprovals,
    IReadOnlyList<string> ApprovedBy);
