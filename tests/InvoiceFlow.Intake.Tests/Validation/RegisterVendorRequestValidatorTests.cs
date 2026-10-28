using FluentValidation.TestHelper;
using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Validation;

namespace InvoiceFlow.Intake.Tests.Validation;

public sealed class RegisterVendorRequestValidatorTests
{
    private readonly RegisterVendorRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var request = new RegisterVendorRequest(
            "Test Vendor",
            "27AAPFU0939F1ZV",
            30);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingName_HasError(string? name)
    {
        var request = new RegisterVendorRequest(
            name,
            "27AAPFU0939F1ZV",
            30);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithInvalidGstin_HasError()
    {
        var request = new RegisterVendorRequest(
            "Test Vendor",
            "INVALID",
            30);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Gstin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(181)]
    public void Validate_WithOutOfRangePaymentTerms_HasError(int days)
    {
        var request = new RegisterVendorRequest(
            "Test Vendor",
            "27AAPFU0939F1ZV",
            days);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PaymentTermsDays);
    }

    [Fact]
    public void Validate_WhenPaymentTermsAreMissing_HasRequiredError()
    {
        var request = new RegisterVendorRequest(
            "Test Vendor",
            "27AAPFU0939F1ZV",
            null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PaymentTermsDays)
            .WithErrorMessage("Payment terms are required.");
    }

    [Fact]
    public void Validate_WithInvalidGstin_HasCorrectMessage()
    {
        var request = new RegisterVendorRequest(
            "Test Vendor",
            "INVALID",
            30);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Gstin)
            .WithErrorMessage("GSTIN is invalid.");
    }
}
