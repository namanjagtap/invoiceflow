using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Infrastructure;

internal interface IVendorRepository
{
    Task<bool> TryAddAsync(
        Vendor vendor,
        CancellationToken cancellationToken);
    Task<Vendor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}
