using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

[Collection(SqlServerCollection.Name)]
public sealed class InvoiceRepositoryTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task TryAddAsync_WhenInvoiceKeyIsUnique_SavesAndPreservesAllFields()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice invoice = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 5, 10),
            TestData.UtcNow);

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new InvoiceRepository(dbContext);

            Assert.True(
                await repository.TryAddAsync(invoice, CancellationToken.None));
        }

        await using (IntakeDbContext dbContext = fixture.CreateDbContext())
        {
            var repository = new InvoiceRepository(dbContext);

            Invoice? savedInvoice = await repository.GetByIdAsync(
                invoice.Id,
                CancellationToken.None);

            Assert.NotNull(savedInvoice);
            Assert.Equal(invoice.Id, savedInvoice.Id);
            Assert.Equal(invoice.VendorId, savedInvoice.VendorId);
            Assert.Equal(invoice.Number, savedInvoice.Number);
            Assert.Equal(invoice.FinancialYear, savedInvoice.FinancialYear);
            Assert.Equal(invoice.TaxableAmount, savedInvoice.TaxableAmount);
            Assert.Equal(invoice.TaxAmount, savedInvoice.TaxAmount);
            Assert.Equal(invoice.TotalAmount, savedInvoice.TotalAmount);
            Assert.Equal(invoice.Status, savedInvoice.Status);
            Assert.Equal(TestData.UtcNow, savedInvoice.ReceivedAtUtc);
            Assert.Equal(DateTimeKind.Utc, savedInvoice.ReceivedAtUtc.Kind);
        }
    }

    [Fact]
    public async Task TryAddAsync_WhenSameVendorNumberAndFinancialYear_ReturnsFalse()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice first = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice duplicate = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 8, 15));

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        Assert.True(
            await repository.TryAddAsync(first, CancellationToken.None));

        Assert.False(
            await repository.TryAddAsync(duplicate, CancellationToken.None));
    }

    [Fact]
    public async Task TryAddAsync_WhenFinancialYearDiffers_ReturnsTrue()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice first = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice nextFinancialYear = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2027, 4, 10));

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        Assert.True(
            await repository.TryAddAsync(first, CancellationToken.None));

        Assert.True(
            await repository.TryAddAsync(
                nextFinancialYear,
                CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_ReturnsInvoicesNewestFirst()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice older = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 5, 10),
            TestData.UtcNow.AddMinutes(-10));

        Invoice newer = TestData.NewInvoice(
            vendor.Id,
            "INV-002",
            new DateOnly(2026, 5, 11),
            TestData.UtcNow);

        await SaveInvoicesAsync(older, newer);

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        InvoicePage result = await repository.SearchAsync(
            new InvoiceQuery(vendor.Id, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task SearchAsync_WhenVendorIdIsSpecified_ReturnsOnlyThatVendorsInvoices()
    {
        Vendor vendorA = await CreateVendorAsync();
        Vendor vendorB = await CreateVendorAsync();

        Invoice invoiceA = TestData.NewInvoice(
            vendorA.Id,
            "INV-001",
            new DateOnly(2026, 5, 10));

        Invoice invoiceB = TestData.NewInvoice(
            vendorB.Id,
            "INV-002",
            new DateOnly(2026, 5, 11));

        await SaveInvoicesAsync(invoiceA, invoiceB);

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        InvoicePage result = await repository.SearchAsync(
            new InvoiceQuery(vendorA.Id, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(invoiceA.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task SearchAsync_WhenDateRangeIsSpecified_IncludesBothBoundaryDates()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice beforeRange = TestData.NewInvoice(
            vendor.Id,
            "INV-001",
            new DateOnly(2026, 5, 9));

        Invoice fromBoundary = TestData.NewInvoice(
            vendor.Id,
            "INV-002",
            new DateOnly(2026, 5, 10));

        Invoice toBoundary = TestData.NewInvoice(
            vendor.Id,
            "INV-003",
            new DateOnly(2026, 5, 20));

        Invoice afterRange = TestData.NewInvoice(
            vendor.Id,
            "INV-004",
            new DateOnly(2026, 5, 21));

        await SaveInvoicesAsync(
            beforeRange,
            fromBoundary,
            toBoundary,
            afterRange);

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        InvoicePage result = await repository.SearchAsync(
            new InvoiceQuery(
                vendor.Id,
                new DateOnly(2026, 5, 10),
                new DateOnly(2026, 5, 20),
                1,
                20),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, invoice => invoice.Id == fromBoundary.Id);
        Assert.Contains(result.Items, invoice => invoice.Id == toBoundary.Id);
    }

    [Fact]
    public async Task SearchAsync_WhenSecondPageIsRequested_ReturnsCorrectItemsAndTotalCount()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice[] invoices = Enumerable.Range(1, 5)
            .Select(i => TestData.NewInvoice(
                vendor.Id,
                $"INV-{i:D3}",
                new DateOnly(2026, 5, 10),
                TestData.UtcNow.AddMinutes(i)))
            .ToArray();

        await SaveInvoicesAsync(invoices);

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        InvoicePage result = await repository.SearchAsync(
            new InvoiceQuery(vendor.Id, null, null, 2, 2),
            CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("INV-003", result.Items[0].Number.Value);
        Assert.Equal("INV-002", result.Items[1].Number.Value);
    }

    [Fact]
    public async Task SearchAsync_WhenRequestedPageIsBeyondEnd_ReturnsEmptyItemsAndActualTotalCount()
    {
        Vendor vendor = await CreateVendorAsync();

        Invoice[] invoices = Enumerable.Range(1, 5)
            .Select(i => TestData.NewInvoice(
                vendor.Id,
                $"INV-{i:D3}",
                new DateOnly(2026, 5, 10),
                TestData.UtcNow.AddMinutes(i)))
            .ToArray();

        await SaveInvoicesAsync(invoices);

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        InvoicePage result = await repository.SearchAsync(
            new InvoiceQuery(vendor.Id, null, null, 9, 2),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
    }

    private async Task<Vendor> CreateVendorAsync()
    {
        Vendor vendor = TestData.NewVendor();

        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new VendorRepository(dbContext);

        Assert.True(
            await repository.TryAddAsync(vendor, CancellationToken.None));

        return vendor;
    }

    private async Task SaveInvoicesAsync(params Invoice[] invoices)
    {
        await using IntakeDbContext dbContext = fixture.CreateDbContext();
        var repository = new InvoiceRepository(dbContext);

        foreach (Invoice invoice in invoices)
        {
            Assert.True(
                await repository.TryAddAsync(invoice, CancellationToken.None));
        }
    }
}
