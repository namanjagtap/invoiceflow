using InvoiceFlow.Approval;
using InvoiceFlow.Intake;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddIntakeModule(builder.Configuration);
builder.Services.AddApprovalModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapIntakeEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();