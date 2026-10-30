using FluentValidation;
using InvoiceFlow.Intake.Authorization;
using InvoiceFlow.Intake.Endpoints;
using InvoiceFlow.Intake.Files;
using InvoiceFlow.Intake.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvoiceFlow.Intake;

public static class IntakeModule
{
    public static IServiceCollection AddIntakeModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IntakeOptions>()
            .Bind(configuration.GetSection(IntakeOptions.SectionName))
            .ValidateDataAnnotations()
            .PostConfigure<IHostEnvironment>((options, env) =>
            {
                if (!string.IsNullOrWhiteSpace(options.StoragePath))
                {
                    options.StoragePath = Path.GetFullPath(options.StoragePath, env.ContentRootPath);
                }
            }).ValidateOnStart();

        services.AddSingleton<IInvoiceFileStore, LocalInvoiceFileStore>();
        services.AddSingleton<IVendorRepository, InMemoryVendorRepository>();
        services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        services.AddValidatorsFromAssembly(typeof(IntakeModule).Assembly, includeInternalTypes: true);
        services.AddIntakePolicies();

        return services;
    }

    public static IEndpointRouteBuilder MapIntakeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/intake");

        group.MapPost("/files", UploadInvoiceFile.HandleAsync)
            .DisableAntiforgery()
            .RequireAuthorization(IntakePolicies.SubmitInvoices);
        group.MapGroup("/vendors")
            .WithTags("Vendors")
            .MapVendorEndpoints();
        group.MapGroup("/invoices")
            .WithTags("Invoices")
            .MapInvoiceEndpoints();

        return endpoints;
    }
}
