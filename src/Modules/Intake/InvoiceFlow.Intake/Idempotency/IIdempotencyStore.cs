using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Intake.Idempotency;

internal interface IIdempotencyStore
{
    IdempotencyStart TryStart(
        string scope,
        string key,
        object request);

    void Complete(
        string scope,
        string key,
        IResult result);

    void Release(
        string scope,
        string key);
}
