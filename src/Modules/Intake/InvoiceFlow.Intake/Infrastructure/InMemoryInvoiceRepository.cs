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

    public InvoicePage Search(InvoiceQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<Invoice> invoices = _invoicesById.Values;

        if (query.VendorId.HasValue)
        {
            invoices = invoices.Where(
                invoice => invoice.VendorId == query.VendorId.Value);
        }

        if (query.FromDate.HasValue)
        {
            invoices = invoices.Where(
                invoice => invoice.InvoiceDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            invoices = invoices.Where(
                invoice => invoice.InvoiceDate <= query.ToDate.Value);
        }

        IOrderedEnumerable<Invoice> orderedInvoices = invoices
            .OrderByDescending(invoice => invoice.ReceivedAtUtc)
            .ThenByDescending(invoice => invoice.Id);

        int totalCount = orderedInvoices.Count();

        Invoice[] items = orderedInvoices
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new InvoicePage(items, totalCount);
    }

    private readonly record struct InvoiceKey(
        Guid VendorId,
        string InvoiceNumber,
        string FinancialYear);
}
