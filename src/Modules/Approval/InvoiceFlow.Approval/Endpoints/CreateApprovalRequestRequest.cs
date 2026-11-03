namespace InvoiceFlow.Approval.Endpoints;

internal sealed record CreateApprovalRequestRequest(
    Guid InvoiceId,
    decimal? Amount);
