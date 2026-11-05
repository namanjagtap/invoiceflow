using FluentValidation;
using InvoiceFlow.Intake.Endpoints;

namespace InvoiceFlow.Intake.Validation;

internal sealed class ListInvoicesRequestValidator : AbstractValidator<ListInvoicesRequest>
{
    public ListInvoicesRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .When(request => request.Page.HasValue)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, ListInvoicesRequest.MaxPageSize)
            .When(request => request.PageSize.HasValue)
            .WithMessage(
                $"Page size must be between 1 and {ListInvoicesRequest.MaxPageSize}.");

        RuleFor(request => request.ToDate)
            .Must((request, toDate) => toDate!.Value >= request.FromDate!.Value)
            .When(request => request.FromDate.HasValue && request.ToDate.HasValue)
            .WithMessage("To date must be on or after from date.");
    }
}
