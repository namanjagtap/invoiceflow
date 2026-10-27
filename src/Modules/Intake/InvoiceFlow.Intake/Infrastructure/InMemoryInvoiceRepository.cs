using InvoiceFlow.Intake.Domain;
using System.Collections.Concurrent;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly ConcurrentDictionary<Guid, Invoice> _invoicesById = new();

    private readonly ConcurrentDictionary<InvoiceKey, Invoice> _invoicesByKey = new();

    public bool TryAdd(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var key = new InvoiceKey(
            invoice.VendorId,
            invoice.Number.Value,
            invoice.FinancialYear.Value);

        // Reject duplicate business keys.
        if (!_invoicesByKey.TryAdd(key, invoice))
        {
            return false;
        }

        // Store the invoice for lookup by its unique ID.
        if (_invoicesById.TryAdd(invoice.Id, invoice))
        {
            return true;
        }

        // Roll back the uniqueness entry if ID insertion fails.
        _invoicesByKey.TryRemove(
            new KeyValuePair<InvoiceKey, Invoice>(key, invoice));

        return false;
    }

    public Invoice? GetById(Guid id)
    {
        return _invoicesById.GetValueOrDefault(id);
    }

    public IReadOnlyList<Invoice> List()
    {
        return _invoicesById.Values
            .OrderByDescending(invoice => invoice.ReceivedAtUtc)
            .ThenByDescending(invoice => invoice.Id)
            .ToArray();
    }

    private readonly record struct InvoiceKey(
        Guid VendorId,
        string InvoiceNumber,
        string FinancialYear);
}
