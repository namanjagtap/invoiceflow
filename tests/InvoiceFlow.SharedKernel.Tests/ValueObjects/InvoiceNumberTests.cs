using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.SharedKernel.Tests.ValueObjects;

public class InvoiceNumberTests
{
    [Theory]
    [InlineData("INV-2026-001", "INV-2026-001")]
    [InlineData(" inv/2026/001 ", "INV/2026/001")]
    [InlineData("abc123", "ABC123")]
    [InlineData("A", "A")]
    [InlineData("1234567890123456", "1234567890123456")]
    public void TryParse_ValidInput_NormalizesValue(
        string input,
        string expected)
    {
        var success = InvoiceNumber.TryParse(input, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901234567")]
    [InlineData("INV 001")]
    [InlineData("INV_001")]
    [InlineData("INV.001")]
    [InlineData("INV@001")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        var success = InvoiceNumber.TryParse(input, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_NullInput_ReturnsFalse()
    {
        var success = InvoiceNumber.TryParse(null, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_ValidInput_ReturnsInvoiceNumber()
    {
        var result = InvoiceNumber.Parse(" inv-001 ");

        Assert.Equal("INV-001", result.Value);
    }

    [Fact]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => InvoiceNumber.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("INV 001")]
    [InlineData("12345678901234567")]
    public void Parse_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(
            () => InvoiceNumber.Parse(input));
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        var result = InvoiceNumber.Parse(" inv/2026-001 ");

        Assert.Equal("INV/2026-001", result.ToString());
    }

    [Fact]
    public void EqualValues_AreEqualRecords()
    {
        var first = InvoiceNumber.Parse("inv-001");
        var second = InvoiceNumber.Parse(" INV-001 ");

        Assert.Equal(first, second);
    }
}
