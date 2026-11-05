namespace InvoiceFlow.Intake.Endpoints;

internal sealed record ListInvoicesRequest(
    int? Page,
    int? PageSize,
    Guid? VendorId,
    DateOnly? FromDate,
    DateOnly? ToDate)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
