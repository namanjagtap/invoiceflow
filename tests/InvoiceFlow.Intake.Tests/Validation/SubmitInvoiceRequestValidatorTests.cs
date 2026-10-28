using FluentValidation.TestHelper;
using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Validation;
using Microsoft.Extensions.Time.Testing;

namespace InvoiceFlow.Intake.Tests.Validation;

public sealed class SubmitInvoiceRequestValidatorTests
{
    private readonly SubmitInvoiceRequestValidator _validator = new(
        new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 28, 6, 0, 0, TimeSpan.Zero)));

    private static SubmitInvoiceRequest ValidRequest() =>
        new(
            Guid.NewGuid(),
            "INV-001",
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 6, 10),
            1000m,
            180m,
            1180m,
            Guid.NewGuid());

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroTax_IsValid()
    {
        var request = ValidRequest() with
        {
            TaxableAmount = 1000m,
            TaxAmount = 0m,
            TotalAmount = 1000m
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.TaxAmount);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNegativeTax_HasError()
    {
        var request = ValidRequest() with
        {
            TaxAmount = -1m
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TaxAmount);
    }

    [Fact]
    public void Validate_WhenTotalDoesNotMatch_HasError()
    {
        var request = ValidRequest() with
        {
            TotalAmount = 2000m
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TotalAmount);
    }

    [Fact]
    public void Validate_WhenDueDateIsBeforeInvoiceDate_HasError()
    {
        var request = ValidRequest() with
        {
            DueDate = new DateOnly(2026, 5, 1)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Validate_WithEmptyVendorId_HasError()
    {
        var request = ValidRequest() with
        {
            VendorId = Guid.Empty
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.VendorId);
    }

    [Fact]
    public void Validate_WhenInvoiceDateIsTodayInIndia_IsValid()
    {
        // 19:00 UTC on 28 October = 00:30 IST on 29 October.
        var clock = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 28, 19, 0, 0, TimeSpan.Zero));

        var validator = new SubmitInvoiceRequestValidator(clock);

        var request = ValidRequest() with
        {
            InvoiceDate = new DateOnly(2026, 10, 29),
            DueDate = new DateOnly(2026, 11, 10)
        };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.InvoiceDate);
    }

    [Fact]
    public void Validate_WhenInvoiceDateIsTomorrowInIndia_HasError()
    {
        var clock = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 10, 28, 19, 0, 0, TimeSpan.Zero));

        var validator = new SubmitInvoiceRequestValidator(clock);

        var request = ValidRequest() with
        {
            InvoiceDate = new DateOnly(2026, 10, 30),
            DueDate = new DateOnly(2026, 11, 10)
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InvoiceDate)
            .WithErrorMessage("Invoice date cannot be in the future.");
    }

    [Fact]
    public void Validate_WhenInvoiceNumberIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { InvoiceNumber = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InvoiceNumber)
            .WithErrorMessage("Invoice number is required.");
    }

    [Fact]
    public void Validate_WhenInvoiceDateIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { InvoiceDate = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InvoiceDate)
            .WithErrorMessage("Invoice date is required.");
    }

    [Fact]
    public void Validate_WhenTaxAmountIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { TaxAmount = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TaxAmount)
            .WithErrorMessage("Tax amount is required.");
    }

    [Fact]
    public void Validate_WhenTaxableAmountIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { TaxableAmount = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TaxableAmount)
            .WithErrorMessage("Taxable amount is required.");
    }

    [Fact]
    public void Validate_WhenTotalAmountIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { TotalAmount = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TotalAmount)
            .WithErrorMessage("Total amount is required.");
    }

    [Fact]
    public void Validate_WhenVendorIdIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { VendorId = Guid.Empty };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.VendorId)
            .WithErrorMessage("Vendor ID is required.");
    }

    [Fact]
    public void Validate_WhenFileIdIsMissing_HasRequiredError()
    {
        var request = ValidRequest() with { FileId = Guid.Empty };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FileId)
            .WithErrorMessage("File ID is required.");
    }

    [Fact]
    public void Validate_WithInvalidInvoiceNumber_HasError()
    {
        var request = ValidRequest() with { InvoiceNumber = "INV 001" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InvoiceNumber)
            .WithErrorMessage("Invoice number is invalid.");
    }

    [Fact]
    public void Validate_WhenTotalDiffersByExactlyOnePaisa_IsValid()
    {
        var request = ValidRequest() with
        {
            TaxableAmount = 1000m,
            TaxAmount = 180m,
            TotalAmount = 1180.01m
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.TotalAmount);
    }

    [Fact]
    public void Validate_WhenTotalDiffersByTwoPaise_HasError()
    {
        var request = ValidRequest() with
        {
            TaxableAmount = 1000m,
            TaxAmount = 180m,
            TotalAmount = 1180.02m
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TotalAmount)
            .WithErrorMessage(
                "Total amount must equal taxable amount plus tax amount within ₹0.01.");
    }
}
