using InvoiceFlow.Intake.Authorization;
using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Intake.Endpoints;

internal static class VendorEndpoints
{
    public static RouteGroupBuilder MapVendorEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapPost("/", RegisterVendor)
            .AddEndpointFilter<ValidationFilter<RegisterVendorRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IntakePolicies.ManageVendors);

        group.MapGet("/{id:guid}", GetVendorById)
            .WithName("GetVendorById")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(IntakePolicies.ReadVendors);

        return group;
    }

    private static Results<CreatedAtRoute<VendorResponse>, ProblemHttpResult> RegisterVendor(
        RegisterVendorRequest request,
        IVendorRepository vendorRepository,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("InvoiceFlow.Intake.Vendors");

        Gstin gstin = Gstin.Parse(request.Gstin!);

        Vendor vendor = Vendor.Register(
            request.Name!,
            gstin,
            request.PaymentTermsDays!.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        if (!vendorRepository.TryAdd(vendor))
        {
            logger.LogWarning(
                "Duplicate vendor registration rejected for GSTIN {Gstin}",
                vendor.Gstin.Value);

            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate vendor",
                detail: "A vendor with this GSTIN is already registered.");
        }

        logger.LogInformation(
            "Vendor {VendorId} registered with GSTIN {Gstin}",
            vendor.Id,
            vendor.Gstin.Value);

        VendorResponse response = ToResponse(vendor);

        return TypedResults.CreatedAtRoute(
            response,
            "GetVendorById",
            new { id = vendor.Id });
    }

    private static Results<Ok<VendorResponse>, ProblemHttpResult> GetVendorById(
        Guid id,
        IVendorRepository vendorRepository)
    {
        Vendor? vendor = vendorRepository.GetById(id);

        if (vendor is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Vendor not found");
        }

        return TypedResults.Ok(ToResponse(vendor));
    }

    private static VendorResponse ToResponse(Vendor vendor)
    {
        return new VendorResponse(
            vendor.Id,
            vendor.Name,
            vendor.Gstin.Value,
            vendor.Gstin.StateCode,
            vendor.PaymentTermsDays);
    }
}
