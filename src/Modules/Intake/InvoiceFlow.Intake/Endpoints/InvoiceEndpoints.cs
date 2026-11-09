using InvoiceFlow.Intake.Authorization;
using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Files;
using InvoiceFlow.Intake.Idempotency;
using InvoiceFlow.Intake.Infrastructure;
using InvoiceFlow.SharedKernel.Http;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InvoiceFlow.Intake.Endpoints;

internal static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapPost("/", SubmitInvoice)
            .AddEndpointFilter<IdempotencyFilter<SubmitInvoiceRequest>>()
            .AddEndpointFilter<ValidationFilter<SubmitInvoiceRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(IntakePolicies.SubmitInvoices)
            .RequireRateLimiting(RateLimitPolicies.Submissions);

        group.MapGet("/{id:guid}", GetInvoiceById)
            .WithName("GetInvoiceById")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IntakePolicies.ReadInvoices);

        group.MapGet("/", ListInvoices)
            .AddEndpointFilter<ValidationFilter<ListInvoicesRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(IntakePolicies.ReadInvoices);

        return group;
    }

    private static async Task<Results<CreatedAtRoute<InvoiceResponse>, ProblemHttpResult, ForbidHttpResult>> SubmitInvoice(
        SubmitInvoiceRequest request,
        IVendorRepository vendorRepository,
        IInvoiceRepository invoiceRepository,
        IInvoiceFileStore fileStore,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger("InvoiceFlow.Intake.Invoices");

        if (!VendorAccess.CanActFor(user, request.VendorId))
        {
            return TypedResults.Forbid();
        }

        Vendor? vendor = vendorRepository.GetById(request.VendorId);

        if (vendor is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unknown vendor",
                detail: "The specified vendor is not registered.");
        }

        bool fileExists = await fileStore.ExistsAsync(
            request.FileId,
            cancellationToken);

        if (!fileExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unknown file",
                detail: "The specified invoice file does not exist.");
        }

        InvoiceNumber number = InvoiceNumber.Parse(request.InvoiceNumber!);

        Invoice invoice = Invoice.Submit(
            request.VendorId,
            number,
            request.InvoiceDate!.Value,
            request.DueDate,
            Money.Inr(request.TaxableAmount!.Value),
            Money.Inr(request.TaxAmount!.Value),
            Money.Inr(request.TotalAmount!.Value),
            request.FileId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (!invoiceRepository.TryAdd(invoice))
        {
            logger.LogWarning(
                "Duplicate invoice {InvoiceNumber} rejected for vendor {VendorId}",
                invoice.Number.Value,
                invoice.VendorId);

            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate invoice",
                detail: "This vendor has already submitted this invoice number in this financial year.");
        }

        logger.LogInformation(
            "Invoice {InvoiceId} ({InvoiceNumber}) submitted by vendor {VendorId} for {TotalAmount}",
            invoice.Id,
            invoice.Number.Value,
            invoice.VendorId,
            invoice.TotalAmount.Amount);

        InvoiceResponse response = ToResponse(invoice);

        return TypedResults.CreatedAtRoute(
            response,
            "GetInvoiceById",
            new { id = invoice.Id });
    }

    private static Results<Ok<InvoiceResponse>, ProblemHttpResult> GetInvoiceById(
        Guid id,
        IInvoiceRepository invoiceRepository,
        ClaimsPrincipal user)
    {
        Invoice? invoice = invoiceRepository.GetById(id);

        if (invoice is null || !VendorAccess.CanActFor(user, invoice.VendorId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Invoice not found");
        }

        return TypedResults.Ok(ToResponse(invoice));
    }

    private static Results<Ok<PagedResponse<InvoiceResponse>>, ForbidHttpResult> ListInvoices(
        [AsParameters] ListInvoicesRequest request,
        IInvoiceRepository invoiceRepository,
        ClaimsPrincipal user)
    {
        if (!VendorAccess.TryResolveVendorScope(
            user,
            request.VendorId,
            out Guid? vendorId))
        {
            return TypedResults.Forbid();
        }

        int page = request.Page ?? 1;
        int pageSize = request.PageSize ?? ListInvoicesRequest.DefaultPageSize;

        InvoicePage invoicePage = invoiceRepository.Search(
            new InvoiceQuery(
                vendorId,
                request.FromDate,
                request.ToDate,
                page,
                pageSize));

        PagedResponse<InvoiceResponse> response = new(
            invoicePage.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            invoicePage.TotalCount);

        return TypedResults.Ok(response);
    }

    private static InvoiceResponse ToResponse(Invoice invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.VendorId,
            invoice.Number.Value,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.TaxableAmount.Amount,
            invoice.TaxAmount.Amount,
            invoice.TotalAmount.Amount,
            invoice.FinancialYear.Value,
            invoice.Status.ToString(),
            invoice.FileId,
            invoice.ReceivedAtUtc);
    }
}
