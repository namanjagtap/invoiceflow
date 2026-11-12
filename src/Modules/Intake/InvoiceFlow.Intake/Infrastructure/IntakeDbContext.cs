using InvoiceFlow.Intake.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Intake.Infrastructure;

internal sealed class IntakeDbContext(DbContextOptions<IntakeDbContext> options) : DbContext(options)
{
    public const string Schema = "intake";
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IntakeDbContext).Assembly);
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }
}
