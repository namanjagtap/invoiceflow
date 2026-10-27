using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

public sealed class InMemoryVendorRepositoryTests
{
    private static readonly DateTime UtcNow =
        new(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TryAdd_WhenGstinIsUnique_ReturnsTrue()
    {
        var repository = new InMemoryVendorRepository();
        Vendor vendor = CreateVendor("27AAPFU0939F1ZV");

        bool added = repository.TryAdd(vendor);

        Assert.True(added);
        Assert.Equal(vendor, repository.GetById(vendor.Id));
    }

    [Fact]
    public void TryAdd_WhenGstinAlreadyExists_ReturnsFalse()
    {
        var repository = new InMemoryVendorRepository();

        Vendor first = CreateVendor("27AAPFU0939F1ZV");
        Vendor second = CreateVendor("27AAPFU0939F1ZV");

        Assert.True(repository.TryAdd(first));
        Assert.False(repository.TryAdd(second));
    }

    private static Vendor CreateVendor(string gstin) =>
        Vendor.Register(
            "Test Vendor",
            Gstin.Parse(gstin),
            30,
            UtcNow);
}
