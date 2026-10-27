using InvoiceFlow.Intake.Domain;
using System.Collections.Concurrent;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed class InMemoryVendorRepository : IVendorRepository
{
    private readonly ConcurrentDictionary<string, Vendor> _vendorsByGstin =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<Guid, Vendor> _vendorsById = new();

    public bool TryAdd(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        if (!_vendorsByGstin.TryAdd(vendor.Gstin.Value, vendor))
        {
            return false;
        }

        if (_vendorsById.TryAdd(vendor.Id, vendor))
        {
            return true;
        }

        _vendorsByGstin.TryRemove(
            new KeyValuePair<string, Vendor>(vendor.Gstin.Value, vendor));

        return false;
    }

    public Vendor? GetById(Guid id)
    {
        return _vendorsById.GetValueOrDefault(id);
    }
}
