using System.Globalization;

namespace InvoiceFlow.SharedKernel.ValueObjects;

public sealed record Money
{
    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = decimal.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money Inr(decimal amount) => Create(amount, "INR");

    public static Money Create(decimal amount, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        string normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(c => c is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency must be a three-letter code", nameof(currency));
        }

        return new Money(amount, normalizedCurrency);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return Create(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return Create(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal rate)
    {
        ArgumentNullException.ThrowIfNull(money);
        return Create(money.Amount * rate, money.Currency);
    }

    public static Money operator *(decimal rate, Money money) => money * rate;

    public override string ToString()
    {
        if (Currency == "INR")
        {
            return Amount.ToString("C", IndianCulture);
        }
        return $"{Currency} {Amount.ToString("N2", IndianCulture)}";
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot perform arithmetic with {left.Currency} and {right.Currency}.");
        }
    }
}
