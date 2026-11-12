using InvoiceFlow.Intake.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Intake.Infrastructure.Configurations;

internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        builder.Property(v => v.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Gstin)
            .HasConversion(
                gstin => gstin.Value,
                value => Gstin.Parse(value))
            .HasMaxLength(15)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(v => v.Gstin)
            .IsUnique();
    }
}
