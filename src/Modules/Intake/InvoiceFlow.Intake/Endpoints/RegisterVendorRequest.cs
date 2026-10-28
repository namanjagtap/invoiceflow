namespace InvoiceFlow.Intake.Endpoints;

internal sealed record RegisterVendorRequest(
    string? Name,
    string? Gstin,
    int? PaymentTermsDays);