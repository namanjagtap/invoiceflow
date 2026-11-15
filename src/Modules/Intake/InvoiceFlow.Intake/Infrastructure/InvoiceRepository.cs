using InvoiceFlow.Intake.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed class InvoiceRepository(IntakeDbContext dbContext) : IInvoiceRepository
{
    public async Task<bool> TryAddAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        bool invoiceExists = await dbContext.Invoices
            .AnyAsync(
                existing =>
                    existing.VendorId == invoice.VendorId
                    && existing.Number == invoice.Number
                    && existing.FinancialYear == invoice.FinancialYear,
                cancellationToken);

        if (invoiceExists)
        {
            return false;
        }

        dbContext.Invoices.Add(invoice);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (SqlServerErrors.IsUniqueViolation(exception))
        {
            dbContext.Entry(invoice).State = EntityState.Detached;

            return false;
        }
    }

    public async Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(
                invoice => invoice.Id == id,
                cancellationToken);
    }

    public async Task<InvoicePage> SearchAsync(
        InvoiceQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Invoice> invoices = dbContext.Invoices
            .AsNoTracking();

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

        int totalCount = await invoices.CountAsync(cancellationToken);

        Invoice[] items = await invoices
            .OrderByDescending(invoice => invoice.ReceivedAtUtc)
            .ThenByDescending(invoice => invoice.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new InvoicePage(items, totalCount);
    }
}
