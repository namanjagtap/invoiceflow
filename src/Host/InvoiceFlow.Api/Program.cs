using InvoiceFlow.Api.ErrorHandling;
using InvoiceFlow.Api.Middleware;
using InvoiceFlow.Approval;
using InvoiceFlow.Extraction;
using InvoiceFlow.Intake;
using InvoiceFlow.Payments;
using InvoiceFlow.Reporting;
using InvoiceFlow.Validation;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, configuration) =>
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    // Add services to the container. 
    builder.Services.AddOpenApi();
    builder.Services.AddIntakeModule(builder.Configuration);
    builder.Services.AddExtractionModule(builder.Configuration);
    builder.Services.AddValidationModule(builder.Configuration);
    builder.Services.AddApprovalModule(builder.Configuration);
    builder.Services.AddPaymentsModule(builder.Configuration);
    builder.Services.AddReportingModule(builder.Configuration);

    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddApiErrorHandling();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseCorrelationId();         // First: every later log line carries the ID
    app.UseRequestTiming();         // Second: logs the completion time of every request
    app.UseExceptionHandler();      // Converts unhandled exceptions into ProblemDetails responses
    app.UseStatusCodePages();       // Adds ProblemDetails bodies to eligible empty error responses

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
            options.SwaggerEndpoint("/openapi/v1.json", "InvoiceFlow API"));
    }

    app.UseHttpsRedirection();

    app.MapIntakeEndpoints();

    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "InvoiceFlow API failed to start");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
