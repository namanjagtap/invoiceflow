using FluentValidation;
using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Validation;

internal sealed class SubmitInvoiceRequestValidator : AbstractValidator<SubmitInvoiceRequest>
{
    private static readonly TimeZoneInfo IndiaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

    public SubmitInvoiceRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.VendorId)
            .NotEmpty()
            .WithMessage("Vendor ID is required.");

        RuleFor(request => request.InvoiceNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Invoice number is required.")
            .Must(value => InvoiceNumber.TryParse(value, out _))
            .WithMessage("Invoice number is invalid.");

        RuleFor(request => request.InvoiceDate)
            .NotNull()
            .WithMessage("Invoice date is required.");

        RuleFor(request => request.InvoiceDate)
            .Must(date => date!.Value <= TodayInIndia(timeProvider))
            .When(request => request.InvoiceDate.HasValue)
            .WithMessage("Invoice date cannot be in the future.");

        RuleFor(request => request.DueDate)
            .Must((request, dueDate) =>
                !dueDate.HasValue || !request.InvoiceDate.HasValue || dueDate.Value >= request.InvoiceDate.Value)
            .WithMessage("Due date cannot be before the invoice date.");

        RuleFor(request => request.TaxableAmount)
            .NotNull()
            .WithMessage("Taxable amount is required.");

        RuleFor(request => request.TaxableAmount)
            .GreaterThan(0m)
            .WithMessage("Taxable amount must be greater than zero.");

        RuleFor(request => request.TaxAmount)
            .NotNull()
            .WithMessage("Tax amount is required.");

        RuleFor(request => request.TaxAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Tax amount cannot be negative.");

        RuleFor(request => request.TotalAmount)
            .NotNull()
            .WithMessage("Total amount is required.");

        RuleFor(request => request.TotalAmount)
            .GreaterThan(0m)
            .WithMessage("Total amount must be greater than zero.");

        RuleFor(request => request.TotalAmount)
            .Must((request, total) =>
                Math.Abs(
                    total!.Value - (request.TaxableAmount!.Value + request.TaxAmount!.Value))
                 <= 0.01m)
            .When(request =>
                request.TotalAmount.HasValue &&
                request.TaxableAmount.HasValue &&
                request.TaxAmount.HasValue)
            .WithMessage("Total amount must equal taxable amount plus tax amount within ₹0.01.");

        RuleFor(request => request.FileId)
            .NotEmpty()
            .WithMessage("File ID is required.");
    }

    private static DateOnly TodayInIndia(TimeProvider timeProvider)
    {
        DateTimeOffset indiaNow = TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            IndiaTimeZone);

        return DateOnly.FromDateTime(indiaNow.DateTime);
    }
}
