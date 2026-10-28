namespace InvoiceFlow.Intake.Endpoints;

internal sealed record SubmitInvoiceRequest(
    Guid VendorId,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    DateOnly? DueDate,
    decimal? TaxableAmount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    Guid FileId);