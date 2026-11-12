using System.Globalization;

namespace InvoiceFlow.Intake.Domain;

internal sealed record FinancialYear
{
    public string Value { get; }

    private FinancialYear(string value)
    {
        Value = value;
    }

    public static FinancialYear From(DateOnly date)
    {
        int startYear = date.Month >= 4
            ? date.Year
            : date.Year - 1;

        int endYear = startYear + 1;

        return new FinancialYear($"{startYear}-{endYear % 100:00}");
    }

    public static FinancialYear Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 7
            && int.TryParse(
                value.AsSpan(0, 4),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int startYear)
            && startYear is >= 1 and <= 9998)
        {
            FinancialYear financialYear = From(
                new DateOnly(startYear, 4, 1));

            if (financialYear.Value == value)
            {
                return financialYear;
            }
        }

        throw new FormatException(
            $"'{value}' is not a valid financial year.");
    }

    public override string ToString() => Value;
}
