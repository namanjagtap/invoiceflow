using InvoiceFlow.Intake.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;

namespace InvoiceFlow.Intake.Tests.Idempotency;

public sealed class InMemoryIdempotencyStoreTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly InMemoryIdempotencyStore _store;

    public InMemoryIdempotencyStoreTests()
    {
        _store = new InMemoryIdempotencyStore(_timeProvider);
    }

    [Fact]
    public void TryStart_WhenKeyIsNew_ReturnsStarted()
    {
        TestRequest request = new("INV-001");

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            request);

        Assert.Equal(IdempotencyOutcome.Started, result.Outcome);
        Assert.Null(result.Result);
    }

    [Fact]
    public void TryStart_WhenSameRequestIsAlreadyProcessing_ReturnsInProgress()
    {
        TestRequest request = new("INV-001");

        _store.TryStart("clerk1", "key-1", request);

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            request);

        Assert.Equal(IdempotencyOutcome.InProgress, result.Outcome);
    }

    [Fact]
    public void TryStart_WhenRequestIsCompleted_ReturnsReplayWithSameResult()
    {
        TestRequest request = new("INV-001");
        IResult originalResult = TypedResults.Ok("Invoice created");

        _store.TryStart("clerk1", "key-1", request);
        _store.Complete("clerk1", "key-1", originalResult);

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            request);

        Assert.Equal(IdempotencyOutcome.Replay, result.Outcome);
        Assert.Same(originalResult, result.Result);
    }

    [Fact]
    public void TryStart_WhenSameKeyHasDifferentRequest_ReturnsPayloadMismatch()
    {
        _store.TryStart(
            "clerk1",
            "key-1",
            new TestRequest("INV-001"));

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            new TestRequest("INV-002"));

        Assert.Equal(
            IdempotencyOutcome.PayloadMismatch,
            result.Outcome);
    }

    [Fact]
    public void TryStart_AfterRelease_ReturnsStarted()
    {
        _store.TryStart(
            "clerk1",
            "key-1",
            new TestRequest("INV-001"));

        _store.Release("clerk1", "key-1");

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            new TestRequest("INV-002"));

        Assert.Equal(IdempotencyOutcome.Started, result.Outcome);
    }

    [Fact]
    public void TryStart_AfterRetentionExpires_ReturnsStarted()
    {
        TestRequest request = new("INV-001");

        _store.TryStart("clerk1", "key-1", request);

        _timeProvider.Advance(InMemoryIdempotencyStore.Retention);

        IdempotencyStart result = _store.TryStart(
            "clerk1",
            "key-1",
            request);

        Assert.Equal(IdempotencyOutcome.Started, result.Outcome);
    }

    [Fact]
    public void TryStart_WhenSameKeyIsUsedByDifferentScope_ReturnsStarted()
    {
        TestRequest request = new("INV-001");

        _store.TryStart("clerk1", "key-1", request);

        IdempotencyStart result = _store.TryStart(
            "clerk2",
            "key-1",
            request);

        Assert.Equal(IdempotencyOutcome.Started, result.Outcome);
    }

    private sealed record TestRequest(string Number);
}
