using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.SharedKernel.Tests.ValueObjects;

public class GstinTests
{
    [Theory]
    [InlineData("27AAPFU0939F1ZV", 27)]
    [InlineData(" 29aagcb7383j1z4 ", 29)]
    [InlineData("23ABCDE1234F1Z8", 23)]
    public void Parse_ValidGstin_ReturnNormalizedValue(string input, int expectedStateCode)
    {
        Gstin gstin = Gstin.Parse(input);

        Assert.Equal(expectedStateCode, gstin.StateCode);
        Assert.Equal(input.Trim().ToUpperInvariant(), gstin.Value);
    }

    [Theory]
    [InlineData("23ABCDE1234F1Z5")]
    [InlineData("27AAPFU0939F1Z")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void TryParse_InvalidGstin_ReturnsFalse(string input)
    {
        bool success = Gstin.TryParse(input, out Gstin? gstin);

        Assert.False(success);
        Assert.Null(gstin);
    }

    [Fact]
    public void Parse_ValidGstin_ExtractsPan()
    {
        Gstin gstin = Gstin.Parse("27AAPFU0939F1ZV");

        Assert.Equal("AAPFU0939F", gstin.Pan);
    }

    [Fact]
    public void IsSameState_DifferentGstinsFromSameState_ReturnsTrue()
    {
        Gstin first = Gstin.Parse("23ABCDE1234F1Z8");
        Gstin second = Gstin.Parse("23AAPFU0939F1Z3");

        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(23, first.StateCode);
        Assert.Equal(23, second.StateCode);
        Assert.True(first.IsSameState(second));
    }

    [Fact]
    public void Parse_InvalidChecksum_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(
            () => Gstin.Parse("23ABCDE1234F1Z5"));
    }

    [Fact]
    public void ToString_ReturnsGstinValue()
    {
        const string expected = "27AAPFU0939F1ZV";

        Gstin gstin = Gstin.Parse(expected);

        Assert.Equal(expected, gstin.ToString());
    }
}