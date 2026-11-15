using InvoiceFlow.Intake.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed class VendorRepository(IntakeDbContext dbContext) : IVendorRepository
{
    public async Task<bool> TryAddAsync(
        Vendor vendor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        bool gstinTaken = await dbContext.Vendors
            .AnyAsync(
                existing => existing.Gstin == vendor.Gstin,
                cancellationToken);

        if (gstinTaken)
        {
            return false;
        }

        dbContext.Vendors.Add(vendor);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (SqlServerErrors.IsUniqueViolation(exception))
        {
            dbContext.Entry(vendor).State = EntityState.Detached;

            return false;
        }
    }

    public async Task<Vendor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vendor => vendor.Id == id,
                cancellationToken);
    }
}
