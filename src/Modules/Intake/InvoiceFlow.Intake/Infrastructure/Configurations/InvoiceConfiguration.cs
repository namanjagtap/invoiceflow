using InvoiceFlow.Intake.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace InvoiceFlow.Intake.Infrastructure.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.Number)
            .HasConversion(
                number => number.Value,
                value => InvoiceNumber.Parse(value))
            .HasMaxLength(16)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(i => i.FinancialYear)
            .HasConversion(
                financialYear => financialYear.Value,
                value => FinancialYear.Parse(value))
            .HasMaxLength(7)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        MapMoney(builder, i => i.TaxableAmount, "TaxableAmount");
        MapMoney(builder, i => i.TaxAmount, "TaxAmount");
        MapMoney(builder, i => i.TotalAmount, "TotalAmount");

        builder.HasIndex(i => new
        {
            i.VendorId,
            i.Number,
            i.FinancialYear
        })
            .IsUnique();
    }

    private static void MapMoney(
        EntityTypeBuilder<Invoice> builder,
        Expression<Func<Invoice, Money?>> property,
        string columnPrefix)
    {
        builder.ComplexProperty(property, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName(columnPrefix)
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName($"{columnPrefix}Currency")
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
        });
    }
}
