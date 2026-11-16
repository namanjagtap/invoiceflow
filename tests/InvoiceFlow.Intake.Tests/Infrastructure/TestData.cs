using InvoiceFlow.Intake.Domain;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

internal static class TestData
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";

    public static readonly DateTime UtcNow =
        new(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc);

    public static Gstin NewGstin()
    {
        string pan =
            $"{RandomCharacters(Letters, 5)}" +
            $"{RandomCharacters(Digits, 4)}" +
            $"{RandomCharacters(Letters, 1)}";

        string firstFourteenCharacters = $"27{pan}1Z";

        const string checkCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        foreach (char checkCharacter in checkCharacters)
        {
            string candidate = firstFourteenCharacters + checkCharacter;

            if (Gstin.TryParse(candidate, out Gstin? gstin))
            {
                return gstin;
            }
        }

        throw new InvalidOperationException(
            "Could not generate a valid GSTIN.");
    }

    public static Vendor NewVendor()
    {
        return Vendor.Register(
            "Test Vendor",
            NewGstin(),
            30,
            UtcNow);
    }

    public static Invoice NewInvoice(
        Guid vendorId,
        string number,
        DateOnly invoiceDate,
        DateTime? receivedAtUtc = null)
    {
        return Invoice.Submit(
            vendorId,
            InvoiceNumber.Parse(number),
            invoiceDate,
            null,
            Money.Inr(1000m),
            Money.Inr(180m),
            Money.Inr(1180m),
            Guid.NewGuid(),
            receivedAtUtc ?? UtcNow);
    }

    private static string RandomCharacters(string alphabet, int length)
    {
        return string.Concat(
            Enumerable.Range(0, length)
                .Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]));
    }
}
