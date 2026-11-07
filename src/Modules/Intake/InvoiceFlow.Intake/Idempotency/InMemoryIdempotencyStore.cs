using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace InvoiceFlow.Intake.Idempotency;

internal sealed class InMemoryIdempotencyStore(TimeProvider timeProvider) : IIdempotencyStore
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<EntryKey, Entry> _entries = new();

    public IdempotencyStart TryStart(string scope, string key, object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EntryKey entryKey = new EntryKey(scope, key);

        while (true)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();

            if (_entries.TryGetValue(entryKey, out Entry? existing))
            {
                if (existing.ExpiresAt <= now)
                {
                    _entries.TryRemove(
                        new KeyValuePair<EntryKey, Entry>(
                            entryKey,
                            existing));

                    continue;
                }

                if (!existing.Request.Equals(request))
                {
                    return new(IdempotencyOutcome.PayloadMismatch);
                }

                if (existing.Result is null)
                {
                    return new(IdempotencyOutcome.InProgress);
                }

                return new(IdempotencyOutcome.Replay, existing.Result);
            }

            Entry newEntry = new(
                request,
                now + Retention,
                null);

            if (_entries.TryAdd(entryKey, newEntry))
            {
                return new(IdempotencyOutcome.Started);
            }
        }
    }

    public void Complete(string scope, string key, IResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        EntryKey entryKey = new EntryKey(scope, key);

        if (_entries.TryGetValue(entryKey, out Entry? existing))
        {
            _entries.TryUpdate(
                entryKey,
                existing with { Result = result },
                existing);
        }
    }

    public void Release(string scope, string key)
    {
        EntryKey entryKey = new(scope, key);

        _entries.TryRemove(entryKey, out _);
    }

    private readonly record struct EntryKey(
        string Scope,
        string Key);

    private sealed record Entry(
        object Request,
        DateTimeOffset ExpiresAt,
        IResult? Result);
}
