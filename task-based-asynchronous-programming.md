# Task-Based Asynchronous Programming (TAP) in C#

## What is a Task?

A `Task` represents an **ongoing operation** — a unit of work that may run asynchronously. It is the foundation of modern .NET concurrency and sits at a higher abstraction level than raw `Thread` objects.

- `Task` — represents an operation with no return value.
- `Task<T>` — represents an operation that produces a value of type `T`.

`Task.Run(...)` submits the work to the **ThreadPool**, which reuses threads instead of creating new OS threads for every request.

---

## 1. Returning Results from Tasks

**Project:** `ReturningResultsFromTask`

Split an array into segments, compute each segment's sum in parallel on a separate `Task<int>`, then aggregate the results.

```csharp
int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

int SumSegment(int start, int end)
{
    int segmentSum = 0;
    for (int i = start; i < end; i++)
    {
        Thread.Sleep(100);  // simulate CPU work
        segmentSum += array[i];
    }
    return segmentSum;
}

Task<int>[] tasks = new Task<int>[4];
tasks[0] = Task.Run(() => SumSegment(0, 2));
tasks[1] = Task.Run(() => SumSegment(2, 5));
tasks[2] = Task.Run(() => SumSegment(5, 7));
tasks[3] = Task.Run(() => SumSegment(7, 10));

Console.WriteLine($"Sum: {tasks.Sum(t => t.Result)}");
```

### `.Result` — blocking access to the return value

`.Result` blocks the calling thread until the task completes and then returns its value. Avoid calling `.Result` on the UI thread or inside an `async` method — use `await` instead to avoid deadlocks.

### When to use
- CPU-bound divide-and-conquer work.
- Parallel data processing where each chunk produces a result to be merged.

---

## 2. `ContinueWith` — Task Continuations

**Project:** `TaskContinuation-ContinueWith`

A continuation is a callback scheduled to run **when a preceding task finishes**. It receives the completed task as its argument, giving access to its result and status.

```csharp
using var client = new HttpClient();
var task = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

task.ContinueWith(t =>
{
    var result = t.Result;
    var doc = JsonDocument.Parse(result);
    var firstPokemon = doc.RootElement
                          .GetProperty("results")[0];

    Console.WriteLine($"Name: {firstPokemon.GetProperty("name")}");
    Console.WriteLine($"URL:  {firstPokemon.GetProperty("url")}");
});

Console.WriteLine("This line runs before the HTTP call finishes.");
```

### Key points
- The continuation runs on a **ThreadPool thread**, not the calling thread.
- The preceding task's result is available through `t.Result` inside the continuation.
- Prefer `async`/`await` for new code — continuations are the lower-level building block that `await` is built on.

### When to use
- When you need fine-grained control over continuation scheduling (`TaskContinuationOptions`).
- When you cannot use `async`/`await` (e.g., constructors, legacy code).

---

## 3. Chaining Continuations and `Unwrap`

**Project:** `TaskConinuation-Chain&Unwrap`

Continuations can be chained: the output of one feeds the input of the next. When a continuation itself returns a `Task`, you get a `Task<Task<T>>`. `.Unwrap()` flattens it into a single `Task<T>`.

```csharp
var taskPokemonListJson   = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

var taskGetFirstUrl = taskPokemonListJson.ContinueWith(t =>
{
    // parse JSON and return the first Pokemon's URL
    return firstPokemon.GetProperty("url").ToString();
});

var taskDetails = taskGetFirstUrl.ContinueWith(t =>
{
    // t.Result is the URL string — start another HTTP call
    return client.GetStringAsync(t.Result);  // returns Task<Task<string>>
}).Unwrap();                                  // flatten to Task<string>

taskDetails.ContinueWith(t =>
{
    var doc = JsonDocument.Parse(t.Result);
    Console.WriteLine($"Name:   {doc.RootElement.GetProperty("name")}");
    Console.WriteLine($"Weight: {doc.RootElement.GetProperty("weight")}");
});
```

### Why `Unwrap`?
`ContinueWith(t => SomeAsyncMethod())` produces a `Task<Task<TResult>>` — an outer task that wraps an inner task. `.Unwrap()` gives you a single `Task<TResult>` that completes when the inner task completes.

### When to use
- Chaining multiple asynchronous HTTP calls without `async`/`await`.
- Building pipeline patterns (fetch → parse → fetch details → display).

---

## 4. Blocking with `Wait`, `WaitAll`, and `.Result`

**Project:** `TaskContinuation-Wait-WaitAll-Result`

These methods block the calling thread until a task (or all tasks) finish. They are the synchronous bridge between a Task and the calling thread.

```csharp
// Wait — block until task is done, no return value
var task = Task.Run(() => { sum += i; });
task.Wait();
Console.WriteLine(sum);

// .Result — block and get the return value
var task = Task.Run(() => ComputeSum());
Console.WriteLine(task.Result);

// Fetch synchronously — only for console apps or top-level scripts
var task = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");
Console.WriteLine(task.Result);
```

### Warning
- Never call `.Wait()` or `.Result` inside an `async` method or on a UI thread — it causes a **deadlock**.
- These are appropriate only in console applications, top-level program entry points, or test infrastructure.

### When to use
- Console apps where `async Main` is not available or desirable.
- Integration tests that must be synchronous.
- Converting an async library to a sync API as a last resort.

---

## 5. `Task.WhenAll` — Aggregate Without Blocking

**Project:** `TaskContinuation-WhenAll`

`Task.WhenAll` returns a new `Task` that completes **only when all supplied tasks have completed**. It does not block the calling thread — it is the non-blocking alternative to calling `.Wait()` on each task in a loop.

```csharp
Task<int>[] tasks = new Task<int>[4];
tasks[0] = Task.Run(() => SumSegment(0, 2));
tasks[1] = Task.Run(() => SumSegment(2, 5));
tasks[2] = Task.Run(() => SumSegment(5, 7));
tasks[3] = Task.Run(() => SumSegment(7, 10));

Task.WhenAll(tasks).ContinueWith(t =>
{
    // t.Result is int[] — the results of all four tasks
    Console.WriteLine($"Total: {t.Result.Sum()}");
});
```

### `WhenAll` vs `WaitAll`

| | `Task.WhenAll` | `Task.WaitAll` |
|---|---|---|
| Blocks calling thread | No | Yes |
| Returns a `Task` | Yes | No (void) |
| `async`-friendly | Yes (`await Task.WhenAll(...)`) | No |

### When to use
- Fanning out multiple parallel operations and reacting when all complete.
- Downloading multiple files, querying multiple APIs, or processing multiple data segments.
- Always prefer this over `WaitAll` in async contexts.

---

## 6. Task Cancellation

**Project:** `TaskCancelation`

`CancellationTokenSource` creates a token you pass into a task. The task periodically checks the token and stops cleanly when cancellation is requested.

```csharp
using var cts = new CancellationTokenSource();
var token = cts.Token;

var task = Task.Run(Work, token);

var input = Console.ReadLine();
if (input == "c") cts.Cancel();

task.Wait();
Console.WriteLine($"Task status: {task.Status}");

void Work()
{
    for (int i = 0; i < 100000; i++)
    {
        token.ThrowIfCancellationRequested();   // throws OperationCanceledException
        Thread.SpinWait(30000000);
    }
}
```

### Automatic cancellation after a timeout

```csharp
using CancellationTokenSource source = new CancellationTokenSource();
source.CancelAfter(10);  // cancel after 10 ms

var json = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon", source.Token);
Console.WriteLine(json.Result);
```

### Task status after cancellation

| Cancellation method | Task.Status |
|---|---|
| `break` (exits loop, no throw) | `RanToCompletion` |
| `throw new OperationCanceledException()` | `Canceled` |
| `token.ThrowIfCancellationRequested()` | `Canceled` |

### When to use
- Any long-running task that the user should be able to cancel.
- HTTP requests with timeouts.
- Background jobs with a graceful shutdown requirement.

---

## 7. Exception Handling in Tasks

**Project:** `ExceptionHandlingInTasks`

Exceptions thrown inside a `Task` are **silently swallowed** unless you observe them. They are stored in the task's `Exception` property as an `AggregateException`.

### Hidden exceptions (bad pattern)
```csharp
var task = client.GetStringAsync("https://invalid-url.co");
task.ContinueWith(t => Console.WriteLine(t.Result));
// If the request fails, the exception is swallowed silently
```

### Observing task status
```csharp
Thread.Sleep(1000);
Console.WriteLine(task1.Status);  // "Faulted"
Console.WriteLine(task2.Status);  // "Faulted" (continuation also faulted)
```

### Iterating multiple exceptions
```csharp
var tasks = new[]
{
    Task.Run(() => throw new InvalidOperationException("invalid")),
    Task.Run(() => throw new ArgumentNullException("null")),
    Task.Run(() => throw new Exception("general"))
};

Task.WhenAll(tasks).ContinueWith(t =>
{
    if (t.IsFaulted)
        foreach (var ex in t.Exception!.InnerExceptions)
            Console.WriteLine(ex.Message);
});
```

### Surfacing exceptions with `.Wait()` or `.Result`
Accessing `.Result` or calling `.Wait()` on a faulted task **re-throws** the `AggregateException`, which you can then catch normally:

```csharp
try
{
    Task.WhenAll(tasks).Wait();
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
        Console.WriteLine(inner.Message);
}
```

### When to use which approach
- Use `.ContinueWith` + `t.IsFaulted` for fire-and-forget tasks.
- Use `await` + `try/catch` in async methods (unwraps `AggregateException` automatically).
- Use `.Wait()` / `.Result` in synchronous contexts where you need to rethrow.

---

## 8. Task Synchronization with `SemaphoreSlim`

**Project:** `TaskSynchronization`

The same `SemaphoreSlim` throttling pattern from `threads-synchronization.md` applied to `Task`-based code. Replaces `Thread` objects with `Task` objects, making the code cleaner and pool-aware.

```csharp
using SemaphoreSlim semaphore = new SemaphoreSlim(3, 3);

Task monitoringTask = new Task(MonitorQueue);
monitoringTask.Start();

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            var input = requestQueue.Dequeue();
            semaphore.Wait();
            Task processingTask = new Task(() => ProcessInput(input));
            processingTask.Start();
        }
    }
}

void ProcessInput(string? input)
{
    try   { Thread.Sleep(2000); Console.WriteLine($"Processed: {input}"); }
    finally { semaphore.Release(); }
}
```

### When to use
- Limiting how many tasks can run concurrently inside a `Task`-based pipeline.
- In `async` code, prefer `semaphore.WaitAsync()` to avoid blocking the thread.

---

## TAP Summary

| API | Purpose |
|---|---|
| `Task.Run(...)` | Offload CPU work to ThreadPool |
| `Task<T>` | Return a value from a background operation |
| `.ContinueWith(...)` | Schedule work after a task completes |
| `.Unwrap()` | Flatten `Task<Task<T>>` into `Task<T>` |
| `.Wait()` / `.Result` | Block until done (sync contexts only) |
| `Task.WhenAll(...)` | Wait for all tasks, non-blocking |
| `Task.WhenAny(...)` | React to the first task that completes |
| `CancellationToken` | Cooperative, structured cancellation |
| `AggregateException` | Container for one or more task exceptions |
