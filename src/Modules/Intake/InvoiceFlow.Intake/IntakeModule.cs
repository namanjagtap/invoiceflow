using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Intake;

public static class IntakeModule
{
    public static IServiceCollection AddIntakeModule(this IServiceCollection services, IConfiguration configuration)
    {
        string rootPath = configuration["Intake:StoragePath"] ?? "App_Data/invoices";

        services.AddSingleton<IInvoiceFileStore>(
            new LocalInvoiceFileStore(Path.GetFullPath(rootPath)));

        return services;
    }

    public static IEndpointRouteBuilder MapIntakeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/intake");

        group.MapPost("/files", UploadInvoiceFile.HandleAsync).DisableAntiforgery();

        return endpoints;
    }
}
