using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Domain;

internal sealed class Vendor
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Gstin Gstin { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }

    private Vendor(
        Guid id,
        string name,
        Gstin gstin,
        int paymentTermsDays,
        DateTime registeredAtUtc)
    {
        Id = id;
        Name = name;
        Gstin = gstin;
        PaymentTermsDays = paymentTermsDays;
        RegisteredAtUtc = registeredAtUtc;
    }

    public static Vendor Register(
        string name,
        Gstin gstin,
        int paymentTermsDays,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gstin);

        string normalizedName = name.Trim();

        if (normalizedName.Length > 200)
        {
            throw new ArgumentException(
                "Vendor name cannot exceed 200 characters.",
                nameof(name));
        }

        if (paymentTermsDays is < 0 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentTermsDays),
                "Payment terms must be between 0 and 180 days.");
        }

        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Registration time must be UTC.",
                nameof(utcNow));
        }

        return new Vendor(
            Guid.CreateVersion7(),
            normalizedName,
            gstin,
            paymentTermsDays,
            utcNow);
    }
}
