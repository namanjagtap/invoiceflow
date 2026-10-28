namespace InvoiceFlow.Intake.Endpoints;

internal sealed record VendorResponse(
    Guid Id,
    string Name,
    string Gstin,
    string StateCode,
    int PaymentTermsDays);