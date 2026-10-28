namespace InvoiceFlow.Intake.Endpoints;

internal sealed record InvoiceResponse(
    Guid Id,
    Guid VendorId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string FinancialYear,
    string Status,
    Guid FileId,
    DateTime ReceivedAtUtc);
