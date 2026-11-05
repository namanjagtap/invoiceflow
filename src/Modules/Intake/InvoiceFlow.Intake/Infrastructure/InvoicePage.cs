using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed record InvoicePage(
    IReadOnlyList<Invoice> Items,
    int TotalCount);
