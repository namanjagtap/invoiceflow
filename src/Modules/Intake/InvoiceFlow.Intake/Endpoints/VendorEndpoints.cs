using InvoiceFlow.Intake.Domain;
using InvoiceFlow.Intake.Infrastructure;
using InvoiceFlow.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvoiceFlow.Intake.Endpoints;

internal static class VendorEndpoints
{
    public static RouteGroupBuilder MapVendorEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapPost("/", RegisterVendor)
            .AddEndpointFilter<ValidationFilter<RegisterVendorRequest>>();

        return group;
    }

    private static IResult RegisterVendor(
        RegisterVendorRequest request,
        IVendorRepository vendorRepository,
        TimeProvider timeProvider)
    {
        Gstin gstin = Gstin.Parse(request.Gstin!);

        Vendor vendor = Vendor.Register(
            request.Name!,
            gstin,
            request.PaymentTermsDays!.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        if (!vendorRepository.TryAdd(vendor))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate vendor",
                detail: "A vendor with this GSTIN is already registered.");
        }

        var response = new VendorResponse(
            vendor.Id,
            vendor.Name,
            vendor.Gstin.Value,
            vendor.Gstin.StateCode,
            vendor.PaymentTermsDays);

        return TypedResults.Created(
            $"/api/intake/vendors/{vendor.Id}",
            response);
    }
}
