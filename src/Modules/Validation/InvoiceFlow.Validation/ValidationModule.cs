using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Validation;

public static class ValidationModule
{
    public static IServiceCollection AddValidationModule(
        this IServiceCollection services,
        IConfiguration configuration) => services;
}
