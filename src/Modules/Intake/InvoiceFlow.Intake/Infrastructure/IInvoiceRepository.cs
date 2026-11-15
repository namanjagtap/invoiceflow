using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Infrastructure;

internal interface IInvoiceRepository
{
    Task<bool> TryAddAsync(
        Invoice invoice,
        CancellationToken cancellationToken);
    Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
    Task<InvoicePage> SearchAsync(
        InvoiceQuery query,
        CancellationToken cancellationToken);
}
