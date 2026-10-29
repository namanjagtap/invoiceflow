namespace InvoiceFlow.Api.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddApiErrorHandling(
        this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
