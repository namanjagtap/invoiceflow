using InvoiceFlow.Api.Middleware;
using InvoiceFlow.Approval;
using InvoiceFlow.Extraction;
using InvoiceFlow.Intake;
using InvoiceFlow.Payments;
using InvoiceFlow.Reporting;
using InvoiceFlow.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddIntakeModule(builder.Configuration);
builder.Services.AddExtractionModule(builder.Configuration);
builder.Services.AddValidationModule(builder.Configuration);
builder.Services.AddApprovalModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCorrelationId();         // first: every later log line carries the ID
app.UseRequestTiming();         // second: logs the completion time of every request

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapIntakeEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();