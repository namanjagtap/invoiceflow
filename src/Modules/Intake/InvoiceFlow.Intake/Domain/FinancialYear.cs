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

    public override string ToString() => Value;
}
