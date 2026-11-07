using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Intake.Idempotency;

internal sealed record IdempotencyStart(
    IdempotencyOutcome Outcome,
    IResult? Result = null);
