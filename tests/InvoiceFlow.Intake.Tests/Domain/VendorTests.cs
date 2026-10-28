using InvoiceFlow.Intake.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Tests.Domain;

public sealed class VendorTests
{
    private static readonly Gstin ValidGstin =
        Gstin.Parse("27AAPFU0939F1ZV");

    private static Vendor CreateVendor(
        string name = "Acme Supplies",
        int paymentTermsDays = 30,
        DateTime? utcNow = null)
    {
        return Vendor.Register(
            name,
            ValidGstin,
            paymentTermsDays,
            utcNow ?? DateTime.UtcNow);
    }

    [Fact]
    public void Register_TrimsName()
    {
        Vendor vendor = CreateVendor(name: "  Acme Supplies  ");

        Assert.Equal("Acme Supplies", vendor.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(180)]
    public void Register_PaymentTermsAtBoundary_Succeeds(
        int paymentTermsDays)
    {
        Vendor vendor = CreateVendor(
            paymentTermsDays: paymentTermsDays);

        Assert.Equal(paymentTermsDays, vendor.PaymentTermsDays);
    }

    [Fact]
    public void Register_WhenNameExceeds200Characters_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateVendor(name: new string('A', 201)));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(181)]
    public void Register_WhenPaymentTermsAreOutOfRange_ThrowsArgumentOutOfRangeException(
        int paymentTermsDays)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateVendor(paymentTermsDays: paymentTermsDays));

        Assert.Equal("paymentTermsDays", exception.ParamName);
    }

    [Fact]
    public void Register_WhenTimestampIsNotUtc_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateVendor(utcNow: DateTime.Now));

        Assert.Equal("utcNow", exception.ParamName);
    }
}
