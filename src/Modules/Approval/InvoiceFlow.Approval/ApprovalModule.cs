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

        return services;
    }
}
