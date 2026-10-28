using FluentValidation;
using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.SharedKernel.ValueObjects;

namespace InvoiceFlow.Intake.Validation;

internal sealed class RegisterVendorRequestValidator : AbstractValidator<RegisterVendorRequest>
{
    public RegisterVendorRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Vendor name is required.")
            .Must(name => name is null || name.Trim().Length <= 200)
            .WithMessage("Vendor name cannot exceed 200 characters.");

        RuleFor(request => request.Gstin)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("GSTIN is required.")
            .Must(value => Gstin.TryParse(value, out _))
            .WithMessage("GSTIN is invalid.");

        RuleFor(request => request.PaymentTermsDays)
            .NotNull()
            .WithMessage("Payment terms are required.")
            .InclusiveBetween(0, 180)
            .WithMessage("Payment terms must be between 0 and 180 days.");
    }
}
