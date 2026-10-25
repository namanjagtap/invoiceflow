using System.Collections.Concurrent;

Log($"Start. SynchronizationContext = {SynchronizationContext.Current?.GetType().Name ?? "none"}");

// Experiment 1: block with .Result in a console app (no SynchronizationContext)
Run("1. .Result, no context", () => GetDataAsync(continueOnCapturedContext: true));

// Experiment 2: the same call, but inside a single-threaded context (like WinForms, WPF or classic ASP.NET)
Run("2. .Result, single-thread context", () => GetDataAsync(continueOnCapturedContext: true), useContext: true);

// Experiment 3: the same context, but the library uses ConfigureAwait(false)
Run("3. .Result, context + ConfigureAwait(false)", () => GetDataAsync(continueOnCapturedContext: false), useContext: true);

// Experiment 4: no blocking at all: async all the way
Log("4. await (async all the way)");
string data = await GetDataAsync(continueOnCapturedContext: true);
Log($"   result = {data}");

static void Run(string name, Func<Task<string>> work, bool useContext = false)
{
    Log(name);
    var previous = SynchronizationContext.Current;
    if (useContext) SynchronizationContext.SetSynchronizationContext(new SingleThreadContext());
    try
    {
        Task<string> task = work();
        // .Result would block forever if deadlocked; Wait(timeout) lets us detect it instead
        bool finished = task.Wait(TimeSpan.FromSeconds(2));
        Log(finished ? $"   result = {task.Result}" : "   DEADLOCK: still waiting after 2 s");
    }
    finally { SynchronizationContext.SetSynchronizationContext(previous); }
}

static async Task<string> GetDataAsync(bool continueOnCapturedContext)
{
    Log("   before await");
    await Task.Delay(300).ConfigureAwait(continueOnCapturedContext);
    Log("   after await");
    return "invoice data";
}

static void Log(string message) =>
    Console.WriteLine($"[thread {Environment.CurrentManagedThreadId,2}] {message}");

// A context that queues continuations for ONE thread to run later, like a UI thread.
// Nobody runs the queue while that thread is blocked, so a queued continuation never runs.
sealed class SingleThreadContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));
}
