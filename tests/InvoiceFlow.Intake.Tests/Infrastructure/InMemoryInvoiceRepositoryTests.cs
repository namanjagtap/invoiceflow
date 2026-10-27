using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

public sealed class InMemoryInvoiceRepositoryTests
{
    private static readonly DateTime UtcNow =
        new(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TryAdd_WhenInvoiceKeyIsUnique_ReturnsTrue()
    {
        var repository = new InMemoryInvoiceRepository();
        Invoice invoice = CreateInvoice(
            Guid.NewGuid(),
            "INV-001",
            new DateOnly(2026, 5, 10));

        Assert.True(repository.TryAdd(invoice));
        Assert.Equal(invoice, repository.GetById(invoice.Id));
    }

    [Fact]
    public void TryAdd_WhenSameVendorNumberAndFinancialYear_ReturnsFalse()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        Invoice first = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice duplicate = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2026, 8, 15));

        Assert.True(repository.TryAdd(first));
        Assert.False(repository.TryAdd(duplicate));
    }

    [Fact]
    public void TryAdd_WhenFinancialYearDiffers_ReturnsTrue()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        Invoice first = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice nextFinancialYear = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2027, 4, 10));

        Assert.True(repository.TryAdd(first));
        Assert.True(repository.TryAdd(nextFinancialYear));
    }

    [Fact]
    public void List_ReturnsInvoicesNewestFirst()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        Invoice older = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2026, 5, 10),
            UtcNow.AddMinutes(-10));

        Invoice newer = CreateInvoice(
            vendorId,
            "INV-002",
            new DateOnly(2026, 5, 11),
            UtcNow);

        repository.TryAdd(older);
        repository.TryAdd(newer);

        IReadOnlyList<Invoice> result = repository.List();

        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    private static Invoice CreateInvoice(
        Guid vendorId,
        string number,
        DateOnly invoiceDate,
        DateTime? receivedAtUtc = null)
    {
        return Invoice.Submit(
            vendorId,
            InvoiceNumber.Parse(number),
            invoiceDate,
            null,
            Money.Inr(1000m),
            Money.Inr(180m),
            Money.Inr(1180m),
            Guid.NewGuid(),
            receivedAtUtc ?? UtcNow);
    }
}
