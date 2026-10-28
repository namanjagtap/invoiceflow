using InvoiceFlow.Intake.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Tests.Domain;

public sealed class InvoiceTests
{
    private static Invoice CreateInvoice(
        Guid? vendorId = null,
        Guid? fileId = null,
        DateOnly? invoiceDate = null,
        DateOnly? dueDate = null,
        Money? taxableAmount = null,
        Money? taxAmount = null,
        Money? totalAmount = null,
        DateTime? utcNow = null)
    {
        return Invoice.Submit(
            vendorId ?? Guid.NewGuid(),
            InvoiceNumber.Parse("INV-001"),
            invoiceDate ?? new DateOnly(2026, 10, 20),
            dueDate ?? new DateOnly(2026, 11, 19),
            taxableAmount ?? Money.Inr(1000m),
            taxAmount ?? Money.Inr(180m),
            totalAmount ?? Money.Inr(1180m),
            fileId ?? Guid.NewGuid(),
            utcNow ?? DateTime.UtcNow);
    }

    [Fact]
    public void Submit_ValidInvoice_SetsExpectedProperties()
    {
        DateTime receivedAtUtc = DateTime.UtcNow;

        Invoice invoice = CreateInvoice(utcNow: receivedAtUtc);

        Assert.Equal(InvoiceStatus.Received, invoice.Status);
        Assert.Equal("2026-27", invoice.FinancialYear.Value);
        Assert.Equal(receivedAtUtc, invoice.ReceivedAtUtc);
    }

    [Fact]
    public void Submit_ZeroTax_Succeeds()
    {
        Invoice invoice = CreateInvoice(
            taxableAmount: Money.Inr(1000m),
            taxAmount: Money.Inr(0m),
            totalAmount: Money.Inr(1000m));

        Assert.Equal(0m, invoice.TaxAmount.Amount);
        Assert.Equal(1000m, invoice.TotalAmount.Amount);
    }

    [Fact]
    public void Submit_TotalOffByOnePaisa_Succeeds()
    {
        Invoice invoice = CreateInvoice(
            taxableAmount: Money.Inr(1000m),
            taxAmount: Money.Inr(180m),
            totalAmount: Money.Inr(1180.01m));

        Assert.Equal(1180.01m, invoice.TotalAmount.Amount);
    }

    [Fact]
    public void Submit_WhenVendorIdIsEmpty_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(vendorId: Guid.Empty));

        Assert.Equal("vendorId", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenFileIdIsEmpty_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(fileId: Guid.Empty));

        Assert.Equal("fileId", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenTimestampIsNotUtc_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(utcNow: DateTime.Now));

        Assert.Equal("utcNow", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenDueDatePrecedesInvoiceDate_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(
                invoiceDate: new DateOnly(2026, 10, 20),
                dueDate: new DateOnly(2026, 10, 19)));

        Assert.Equal("dueDate", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenTaxableAmountIsZero_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateInvoice(taxableAmount: Money.Inr(0m)));

        Assert.Equal("taxableAmount", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenTaxAmountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateInvoice(taxAmount: Money.Inr(-1m)));

        Assert.Equal("taxAmount", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenTotalDiffersByMoreThanPaisa_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(
                taxableAmount: Money.Inr(1000m),
                taxAmount: Money.Inr(180m),
                totalAmount: Money.Inr(1180.02m)));

        Assert.Equal("totalAmount", exception.ParamName);
    }

    [Fact]
    public void Submit_WhenMoneyIsNotInr_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateInvoice(taxableAmount: Money.Create(1000m, "USD")));

        Assert.Equal("taxableAmount", exception.ParamName);
    }
}
