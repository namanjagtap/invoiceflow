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
    public void Search_ReturnsInvoicesNewestFirst()
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

        InvoicePage result = repository.Search(
            new InvoiceQuery(null, null, null, 1, 20));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
    }

    [Fact]
    public void Search_WhenVendorIdIsSpecified_ReturnsOnlyThatVendorsInvoices()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorA = Guid.NewGuid();
        Guid vendorB = Guid.NewGuid();

        Invoice invoiceA = CreateInvoice(
            vendorA,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice invoiceB = CreateInvoice(
            vendorB,
            "INV-002",
            new DateOnly(2026, 5, 11));

        repository.TryAdd(invoiceA);
        repository.TryAdd(invoiceB);

        InvoicePage result = repository.Search(
            new InvoiceQuery(vendorA, null, null, 1, 20));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(invoiceA.Id, result.Items[0].Id);
    }

    [Fact]
    public void Search_WhenDateRangeIsSpecified_IncludesBothBoundaryDates()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        Invoice beforeRange = CreateInvoice(
            vendorId,
            "INV-001",
            new DateOnly(2026, 5, 9));

        Invoice fromBoundary = CreateInvoice(
            vendorId,
            "INV-002",
            new DateOnly(2026, 5, 10));

        Invoice toBoundary = CreateInvoice(
            vendorId,
            "INV-003",
            new DateOnly(2026, 5, 20));

        Invoice afterRange = CreateInvoice(
            vendorId,
            "INV-004",
            new DateOnly(2026, 5, 21));

        repository.TryAdd(beforeRange);
        repository.TryAdd(fromBoundary);
        repository.TryAdd(toBoundary);
        repository.TryAdd(afterRange);

        InvoicePage result = repository.Search(
            new InvoiceQuery(
                null,
                new DateOnly(2026, 5, 10),
                new DateOnly(2026, 5, 20),
                1,
                20));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, invoice => invoice.Id == fromBoundary.Id);
        Assert.Contains(result.Items, invoice => invoice.Id == toBoundary.Id);
    }

    [Fact]
    public void Search_WhenSecondPageIsRequested_ReturnsCorrectItemsAndTotalCount()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            Invoice invoice = CreateInvoice(
                vendorId,
                $"INV-{i:D3}",
                new DateOnly(2026, 5, 10),
                UtcNow.AddMinutes(i));

            repository.TryAdd(invoice);
        }

        InvoicePage result = repository.Search(
            new InvoiceQuery(null, null, null, 2, 2));

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);

        // Newest first: page 2 contains the 3rd and 4th newest invoices.
        Assert.Equal("INV-003", result.Items[0].Number.Value);
        Assert.Equal("INV-002", result.Items[1].Number.Value);
    }

    [Fact]
    public void Search_WhenRequestedPageIsBeyondEnd_ReturnsEmptyItemsAndActualTotalCount()
    {
        var repository = new InMemoryInvoiceRepository();
        Guid vendorId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            Invoice invoice = CreateInvoice(
                vendorId,
                $"INV-{i:D3}",
                new DateOnly(2026, 5, 10),
                UtcNow.AddMinutes(i));

            repository.TryAdd(invoice);
        }

        InvoicePage result = repository.Search(
            new InvoiceQuery(null, null, null, 9, 2));

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
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
