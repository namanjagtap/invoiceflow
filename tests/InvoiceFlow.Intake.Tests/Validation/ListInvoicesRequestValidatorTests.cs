using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Validation;

namespace InvoiceFlow.Intake.Tests.Validation;

public sealed class ListInvoicesRequestValidatorTests
{
    private readonly ListInvoicesRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenAllPropertiesAreNull_ReturnsValid()
    {
        ListInvoicesRequest request = new(
            null,
            null,
            null,
            null,
            null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPageIsZero_ReturnsValidationError()
    {
        ListInvoicesRequest request = new(
            0,
            null,
            null,
            null,
            null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(ListInvoicesRequest.Page));
    }

    [Fact]
    public void Validate_WhenPageSizeIsZero_ReturnsValidationError()
    {
        ListInvoicesRequest request = new(
            null,
            0,
            null,
            null,
            null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(ListInvoicesRequest.PageSize));
    }

    [Fact]
    public void Validate_WhenPageSizeExceedsMaximum_ReturnsValidationError()
    {
        ListInvoicesRequest request = new(
            null,
            101,
            null,
            null,
            null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(ListInvoicesRequest.PageSize));
    }

    [Fact]
    public void Validate_WhenToDateIsBeforeFromDate_ReturnsValidationError()
    {
        ListInvoicesRequest request = new(
            null,
            null,
            null,
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 5, 10));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(ListInvoicesRequest.ToDate));
    }

    [Fact]
    public void Validate_WhenFromDateAndToDateAreEqual_ReturnsValid()
    {
        DateOnly date = new(2026, 5, 10);

        ListInvoicesRequest request = new(
            null,
            null,
            null,
            date,
            date);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
