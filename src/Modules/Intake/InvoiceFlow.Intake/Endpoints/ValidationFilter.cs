using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Intake.Endpoints;

internal sealed class ValidationFilter<T>(
    IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        T? request = context.Arguments.OfType<T>().FirstOrDefault();

        if (request is null)
        {
            return TypedResults.BadRequest();
        }

        var result = await validator.ValidateAsync(
            request,
            context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
