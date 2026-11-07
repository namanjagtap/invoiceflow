namespace InvoiceFlow.Intake.Idempotency;

internal enum IdempotencyOutcome
{
    Started,
    Replay,
    InProgress,
    PayloadMismatch
}
