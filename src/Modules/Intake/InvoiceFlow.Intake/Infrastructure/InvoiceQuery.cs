namespace InvoiceFlow.Intake.Infrastructure;

internal sealed record InvoiceQuery(
    Guid? VendorId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize);
