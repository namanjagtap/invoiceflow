using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Infrastructure;

internal interface IInvoiceRepository
{
    bool TryAdd(Invoice invoice);
    Invoice? GetById(Guid id);
    IReadOnlyList<Invoice> List();
}
