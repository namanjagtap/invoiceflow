using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceFlow.SharedKernel.ValueObjects;

public sealed partial record Gstin
{
    private const string GstinPattern = @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]\z";

    public string Value { get; }
    public int StateCode => int.Parse(Value[..2], CultureInfo.InvariantCulture);

    public string Pan => Value.Substring(2, 10);

    private Gstin(string value)
    {
        Value = value;
    }

    [GeneratedRegex(GstinPattern, RegexOptions.CultureInvariant)]
    private static partial Regex GstinRegex();

    public static Gstin Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TryParse(value, out Gstin? gstin))
        {
            throw new FormatException($"'{value}' is not a valid GSTIN.");
        }

        return gstin;
    }

    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out Gstin? gstin)
    {
        gstin = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 15)
        {
            return false;
        }

        if (!GstinRegex().IsMatch(normalized))
        {
            return false;
        }

        if (!HasValidChecksum(normalized))
        {
            return false;
        }

        gstin = new Gstin(normalized);

        return true;
    }

    public bool IsSameState(Gstin other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return StateCode == other.StateCode;
    }

    public override string ToString() => Value;

    private static bool HasValidChecksum(string value)
    {
        int sum = 0;

        for (int i = 0; i < 14; i++)
        {
            int characterValue = ToBase36(value[i]);

            int multiplier = i % 2 == 0 ? 1 : 2;
            int product = characterValue * multiplier;

            sum += product / 36 + product % 36;
        }

        int checkValue = (36 - sum % 36) % 36;
        char expectedCheckCharacter = FromBase36(checkValue);

        return value[14] == expectedCheckCharacter;
    }

    private static int ToBase36(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }
        return character - 'A' + 10;
    }

    private static char FromBase36(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }
        return (char)('A' + value - 10);
    }
}
