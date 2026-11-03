using InvoiceFlow.Approval.Authorization;
using InvoiceFlow.Approval.Endpoints;
using InvoiceFlow.Approval.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Approval;

public static class ApprovalModule
{
    public static IServiceCollection AddApprovalModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<
            IValidateOptions<ApprovalOptions>,
            ApprovalOptionsValidator>();

        services.AddOptions<ApprovalOptions>()
            .Bind(configuration.GetSection(ApprovalOptions.SectionName),
            binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        services.AddSingleton<IApprovalPolicy, ApprovalPolicy>();

        services.AddSingleton<IApprovalRequestRepository, InMemoryApprovalRequestRepository>();

        services.AddApprovalPolicies();

        return services;
    }

    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/approval/requests")
            .WithTags("Approval requests");

        group.MapApprovalRequestEndpoints();

        return endpoints;
    }
}
