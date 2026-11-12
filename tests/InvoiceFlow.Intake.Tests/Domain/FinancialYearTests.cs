using InvoiceFlow.Intake.Domain;

namespace InvoiceFlow.Intake.Tests.Domain;

public sealed class FinancialYearTests
{
    public static TheoryData<DateOnly, string> FinancialYearCases => new()
    {
        { new DateOnly(2026, 3, 31), "2025-26" },
        { new DateOnly(2026, 4, 1), "2026-27" },
        { new DateOnly(2027, 1, 15), "2026-27" },
        { new DateOnly(1999, 4, 1), "1999-00" }
    };

    [Theory]
    [MemberData(nameof(FinancialYearCases))]
    public void From_ReturnsExpectedFinancialYear(
        DateOnly date,
        string expected)
    {
        FinancialYear financialYear = FinancialYear.From(date);

        Assert.Equal(expected, financialYear.Value);
    }

    [Theory]
    [InlineData("2025-26")]
    [InlineData("1999-00")]
    public void Parse_ValidValue_EqualsFinancialYearFromDate(string value)
    {
        FinancialYear result = FinancialYear.Parse(value);

        Assert.Equal(
            FinancialYear.From(new DateOnly(int.Parse(value[..4]), 4, 1)),
            result);

        Assert.Equal(value, result.Value);
    }

    [Theory]
    [InlineData("2026-28")]
    [InlineData("2026/27")]
    [InlineData("26-27")]
    [InlineData("0000-01")]
    [InlineData("abcd-ef")]
    public void Parse_InvalidValue_ThrowsFormatException(string value)
    {
        Assert.Throws<FormatException>(
            () => FinancialYear.Parse(value));
    }
}
