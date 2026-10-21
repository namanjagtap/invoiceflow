using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.SharedKernel.Tests.ValueObjects;

public class MoneyTests
{
    public static TheoryData<decimal, decimal> RoundingCases => new()
    {
        { 10.005m, 10.01m },
        { 10.004m, 10.00m },
        { -10.005m, -10.01m }
    };

    [Theory]
    [MemberData(nameof(RoundingCases))]
    public void Inr_RoundsToTwoDecimalsAwayFromZero(decimal input, decimal expected)
    {
        Money money = Money.Inr(input);

        Assert.Equal(expected, money.Amount);
        Assert.Equal("INR", money.Currency);
    }

    [Theory]
    [InlineData("RUPEE")]
    [InlineData("US")]
    [InlineData("U5D")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidCurrency_ThrowsArgumentException(
        string currency)
    {
        Assert.Throws<ArgumentException>(
            () => Money.Create(100m, currency));
    }


    [Fact]
    public void Multiply_AppliesRate()
    {
        Money result = Money.Inr(10_000m) * 0.09m;

        Assert.Equal(900.00m, result.Amount);
        Assert.Equal("INR", result.Currency);
    }

    [Fact]
    public void Multiply_NullMoney_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = (Money)null! * 2m);
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        Money result = Money.Inr(100m) + Money.Inr(250m);

        Assert.Equal(350m, result.Amount);
    }

    [Fact]
    public void Subtract_AllowsNegativeAmounts()
    {
        Money result = Money.Inr(100m) - Money.Inr(250m);

        Assert.Equal(-150m, result.Amount);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        Money inr = Money.Inr(100m);
        Money usd = Money.Create(10m, "USD");

        Assert.Throws<InvalidOperationException>(
            () => inr + usd);
    }

    [Fact]
    public void Subtract_DifferentCurrencies_Throws()
    {
        Money inr = Money.Inr(100m);
        Money usd = Money.Create(10m, "USD");

        Assert.Throws<InvalidOperationException>(
            () => inr - usd);
    }

    [Fact]
    public void Create_NormalizesCurrencyCode()
    {
        Money money = Money.Create(100m, " usd ");

        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void ToString_UsesIndianCurrencyFormatting()
    {
        Money money = Money.Inr(100_000m);

        Assert.Equal("₹1,00,000.00", money.ToString());
    }

    [Fact]
    public void EqualValues_AreEqualRecords()
    {
        Money first = Money.Inr(500m);
        Money second = Money.Inr(500m);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentCurrencies_AreNotEqual()
    {
        Money inr = Money.Inr(500m);
        Money usd = Money.Create(500m, "USD");

        Assert.NotEqual(inr, usd);
    }

    [Fact]
    public void ToString_NegativeAmount_UsesIndianCurrencyFormatting()
    {
        var money = Money.Inr(-150m);

        Assert.Equal("-₹150.00", money.ToString());
    }
}