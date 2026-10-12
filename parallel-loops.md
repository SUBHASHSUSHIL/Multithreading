# Parallel Loops in C#

## What are Parallel Loops?

The `Parallel` class (in `System.Threading.Tasks`) provides data-parallel versions of `for` and `foreach` loops. It automatically partitions the work across ThreadPool threads and manages synchronization of loop state.

Use parallel loops for **CPU-bound work** over independent data items where the per-item cost justifies the overhead of parallelism. For I/O-bound work, use `async`/`await` instead.

---

## 1. `Parallel.For` and `Parallel.ForEach`

**Project:** `parallel-loops-overview`

### `Parallel.For`

```csharp
int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
int sum = 0;
object lockSum = new object();

Parallel.For(0, array.Length, i =>
{
    lock (lockSum)
    {
        sum += array[i];
    }
});

Console.WriteLine($"Sum: {sum}");
```

### `Parallel.ForEach`

```csharp
Parallel.ForEach(array, item =>
{
    lock (lockSum)
    {
        sum += item;
    }
});
```

### `Parallel.Invoke` — run independent delegates in parallel

```csharp
Parallel.Invoke(
    () => Console.WriteLine("Task one"),
    () => Console.WriteLine("Task two"),
    () => Console.WriteLine("Task three")
);
```

All three overloads block the calling thread until all work is complete.

### When to use
- Summing, transforming, or filtering large arrays.
- Independent CPU-bound processing of collection items.
- Running a fixed set of unrelated operations concurrently.

### When NOT to use
- I/O-bound work — use `async/await`.
- Operations with heavy shared state — the lock overhead will serialize threads.
- Collections smaller than ~1000 items — the overhead of parallelism outweighs the benefit.

---

## 2. Thread-Local Storage in Parallel Loops

**Project:** `thread-local-storage`

Locking on every iteration is expensive. Instead, each thread accumulates into a **thread-local variable** and merges only once per thread at the end.

```csharp
int sum = 0;
object lockSum = new object();

Parallel.For(
    0,
    array.Length,
    () => 0,                        // (1) initialize thread-local state
    (i, state, tls) =>
    {
        tls += array[i];            // (2) update thread-local — no lock needed
        return tls;
    },
    tls =>
    {
        lock (lockSum)              // (3) merge into shared state — one lock per thread
        {
            sum += tls;
            Console.WriteLine($"Task id: {Task.CurrentId}");
        }
    }
);
```

The three-argument `Parallel.For` overload accepts:
1. `localInit` — factory for the thread-local initial value.
2. `body` — receives `(index, ParallelLoopState, threadLocalValue)`, returns the new thread-local value.
3. `localFinally` — runs once per thread after all its iterations finish.

### When to use
- Aggregation inside parallel loops (sum, max, count, etc.).
- Any scenario where per-iteration locking creates measurable contention.

---

## 3. Breaking Out of a Parallel Loop

**Project:** `parallel-loops-break`

`state.Break()` signals that no iterations **with an index higher than the current one** need to run. Iterations already in progress on other threads continue to completion.

```csharp
Parallel.For(0, array.Length, (i, state) =>
{
    lock (lockSum)
    {
        // Skip if we should exit and this iteration is past the break point
        if (state.ShouldExitCurrentIteration && state.LowestBreakIteration < i)
            return;

        if (i == 65)
            state.Break();  // no iterations > 65 are needed

        sum += array[i];
    }
});
```

### `state.LowestBreakIteration`
After `Break()`, `state.LowestBreakIteration` contains the lowest index at which `Break()` was called. All iterations below that index are guaranteed to run.

### `Break` vs `Stop`

| | `state.Break()` | `state.Stop()` |
|---|---|---|
| Guarantee | Iterations below the break index complete | No guarantee — stop as soon as possible |
| `state.ShouldExitCurrentIteration` | Set | Set |
| `state.IsStopped` | Not set | Set |
| Use case | Need results up to a certain index | Need any result; order doesn't matter |

### When to use `Break`
- Searching a sorted dataset — once you find the target index, no higher index can match.
- Processing a prioritized queue where you only need the first N results.

---

## 4. Stopping a Parallel Loop

**Project:** `parallel-loops-stop`

`state.Stop()` is more aggressive than `Break()`. It signals all threads to stop as soon as their current iteration finishes, with no ordering guarantees.

```csharp
Parallel.For(0, array.Length, (i, state) =>
{
    lock (lockSum)
    {
        if (!state.IsStopped)
        {
            if (i == 65)
                state.Stop();   // stop all threads ASAP

            sum += array[i];
        }
    }
});
```

### When to use `Stop`
- Finding the first item that matches a condition (order doesn't matter).
- Aborting processing when an error is detected and partial results are acceptable.

---

## 5. Cancelling a Parallel Loop

**Project:** `parallel-loops-cancelation`

Pass a `CancellationToken` via `ParallelOptions` to cancel the entire loop from outside — for example, in response to a user pressing a button.

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
    var options = new ParallelOptions { CancellationToken = cts.Token };

    try
    {
        Parallel.For(0, 100000, options, i =>
        {
            Console.WriteLine($"{DateTime.Now}");
            Thread.SpinWait(30000000);
        });
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Loop was cancelled.");
    }
}
```

When the token is cancelled, `Parallel.For` throws `OperationCanceledException` (wrapped in `AggregateException` if caught at the task level).

### When to use
- Background loops that should respond to user-initiated cancellation.
- Loops with time limits (combine with `CancellationTokenSource(TimeSpan)`).

---

## 6. Exception Handling in Parallel Loops

**Project:** `parallel-loops-exceptions`

All exceptions thrown by iterations are collected and rethrown as a single `AggregateException` after the loop completes.

```csharp
try
{
    Parallel.For(0, array.Length, (i, state) =>
    {
        lock (lockSum)
        {
            if (!state.IsExceptional)   // don't process after an exception
            {
                if (i == 65)
                    throw new InvalidOperationException("Intentional error at 65.");

                sum += array[i];
            }
        }
    });
}
catch (AggregateException ex)
{
    Console.WriteLine(ex);  // contains all thrown exceptions as InnerExceptions
}
```

### `state.IsExceptional`
Becomes `true` when any iteration has thrown an unhandled exception. Use it to skip further processing gracefully.

### Iterating inner exceptions

```csharp
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
        Console.WriteLine(inner.Message);
}
```

### When to use
- Any parallel loop where individual iterations might fail independently.
- Always wrap `Parallel.For`/`ForEach` in `try/catch (AggregateException)` in production code.

---

## `Parallel` Loop — Decision Guide

```
Is the work CPU-bound?
├── No (I/O: HTTP, file, DB)  →  Use async/await
└── Yes
    ├── Do you need all items processed?
    │   ├── Yes  →  Parallel.ForEach / Parallel.For
    │   └── No (stop at condition)
    │       ├── Order matters  →  state.Break()
    │       └── Order doesn't matter  →  state.Stop()
    ├── Should user be able to cancel?  →  ParallelOptions.CancellationToken
    ├── Aggregation result needed?  →  Thread-local storage pattern
    └── Unrelated independent actions  →  Parallel.Invoke
```
