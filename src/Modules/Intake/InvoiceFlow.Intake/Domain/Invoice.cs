using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Domain;

internal sealed class Invoice
{
    private const decimal AmountTolerance = 0.01m;

    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public InvoiceNumber Number { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Money TaxableAmount { get; private set; }
    public Money TaxAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public Guid FileId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public FinancialYear FinancialYear { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    private Invoice(
        Guid id,
        Guid vendorId,
        InvoiceNumber number,
        DateOnly invoiceDate,
        DateOnly? dueDate,
        Money taxableAmount,
        Money taxAmount,
        Money totalAmount,
        Guid fileId,
        InvoiceStatus status,
        FinancialYear financialYear,
        DateTime receivedAtUtc)
    {
        Id = id;
        VendorId = vendorId;
        Number = number;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        TaxableAmount = taxableAmount;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        FileId = fileId;
        Status = status;
        FinancialYear = financialYear;
        ReceivedAtUtc = receivedAtUtc;
    }

    public static Invoice Submit(
        Guid vendorId,
        InvoiceNumber number,
        DateOnly invoiceDate,
        DateOnly? dueDate,
        Money taxableAmount,
        Money taxAmount,
        Money totalAmount,
        Guid fileId,
        DateTime utcNow)
    {
        if (vendorId == Guid.Empty)
        {
            throw new ArgumentException(
                "Vendor ID cannot be empty.",
                nameof(vendorId));
        }

        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(taxableAmount);
        ArgumentNullException.ThrowIfNull(taxAmount);
        ArgumentNullException.ThrowIfNull(totalAmount);

        if (fileId == Guid.Empty)
        {
            throw new ArgumentException(
                "File ID cannot be empty.",
                nameof(fileId));
        }

        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Received time must be UTC.",
                nameof(utcNow));
        }

        if (dueDate.HasValue && dueDate.Value < invoiceDate)
        {
            throw new ArgumentException(
                "Due date cannot be before the invoice date.",
                nameof(dueDate));
        }

        if (taxableAmount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxableAmount),
                "Taxable amount must be greater than zero.");
        }

        if (taxAmount.Amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxAmount),
                "Tax amount cannot be negative.");
        }

        if (totalAmount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount),
                "Total amount must be greater than zero.");
        }

        EnsureInr(taxableAmount, nameof(taxableAmount));
        EnsureInr(taxAmount, nameof(taxAmount));
        EnsureInr(totalAmount, nameof(totalAmount));

        decimal expectedTotal = (taxableAmount + taxAmount).Amount;
        decimal difference = Math.Abs(totalAmount.Amount - expectedTotal);

        if (difference > AmountTolerance)
        {
            throw new ArgumentException(
                "Total amount must equal taxable amount plus tax amount within ₹0.01.",
                nameof(totalAmount));
        }

        return new Invoice(
            Guid.CreateVersion7(),
            vendorId,
            number,
            invoiceDate,
            dueDate,
            taxableAmount,
            taxAmount,
            totalAmount,
            fileId,
            InvoiceStatus.Received,
            FinancialYear.From(invoiceDate),
            utcNow);
    }

    private static void EnsureInr(Money amount, string parameterName)
    {
        if (amount.Currency != "INR")
        {
            throw new ArgumentException(
                "Invoice amounts must be in INR.",
                parameterName);
        }
    }
}
