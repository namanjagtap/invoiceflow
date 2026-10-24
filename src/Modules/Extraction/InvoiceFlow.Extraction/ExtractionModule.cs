using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Extraction;

public static class ExtractionModule
{
    public static IServiceCollection AddExtractionModule(
        this IServiceCollection services,
        IConfiguration configuration) => services;
}
