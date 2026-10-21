using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace InvoiceFlow.SharedKernel.ValueObjects;

public sealed partial record InvoiceNumber
{
    public string Value { get; }

    private InvoiceNumber(string value)
    {
        Value = value;
    }

    [GeneratedRegex(@"^[A-Z0-9/-]{1,16}\z")]
    private static partial Regex InvoiceNumberRegex();

    public static InvoiceNumber Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!TryParse(input, out var invoiceNumber))
        {
            throw new FormatException(
                $"'{input}' is not a valid invoice number. Use 1–16 letters, digits, '-' or '/'.");
        }

        return invoiceNumber;
    }

    public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out InvoiceNumber? invoiceNumber)
    {
        invoiceNumber = null;

        if (input is null)
        {
            return false;
        }

        var normalizedInput = input.Trim().ToUpperInvariant();

        if (!InvoiceNumberRegex().IsMatch(normalizedInput))
        {
            return false;
        }

        invoiceNumber = new InvoiceNumber(normalizedInput);

        return true;
    }

    public override string ToString() => Value;
}
