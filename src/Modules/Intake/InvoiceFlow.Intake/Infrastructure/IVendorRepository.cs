using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Infrastructure;

internal interface IVendorRepository
{
    bool TryAdd(Vendor vendor);
    Vendor? GetById(Guid id);
}
