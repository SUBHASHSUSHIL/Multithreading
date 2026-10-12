# PLINQ — Parallel LINQ in C#

## What is PLINQ?

**PLINQ (Parallel LINQ)** is the parallel version of LINQ to Objects. Adding `.AsParallel()` to any `IEnumerable<T>` query instructs the runtime to partition the source sequence, process partitions concurrently on ThreadPool threads, and merge the results.

PLINQ is ideal for **CPU-bound data transformation and filtering** pipelines that already use LINQ syntax.

---

## 1. Basic PLINQ — `AsParallel`, `Where`, `ForAll`

**Project:** `basic-of-PLINQ`

```csharp
var items = Enumerable.Range(1, 200);

var evenNumbers = items
    .AsParallel()
    .Where(x =>
    {
        Console.WriteLine($"Processing {x}; Thread: {Thread.CurrentThread.ManagedThreadId}");
        return (x % 2 == 0);
    });

// Option A: ForAll — parallel consumer, no ordering guarantee
evenNumbers.ForAll(item =>
{
    Console.WriteLine($"Result: {item}; Thread: {Thread.CurrentThread.ManagedThreadId}");
});
```

### `.AsParallel()`

Converts an `IEnumerable<T>` into a `ParallelQuery<T>`. All subsequent LINQ operators (`.Where`, `.Select`, `.OrderBy`, etc.) run in parallel.

### `.ForAll()` vs `foreach`

| | `.ForAll(action)` | `foreach` |
|---|---|---|
| Execution | Parallel | Sequential |
| Thread | Multiple ThreadPool threads | Calling thread |
| Order | Not preserved | Preserved (with `AsOrdered`) |
| Use for | Fire-and-forget processing | When order matters |

### When to use `.AsParallel()`
- Filtering or transforming large in-memory collections (thousands of items).
- CPU-heavy predicates or selectors (image processing, text parsing, computation).
- When sequential LINQ is already the bottleneck and profiling confirms it.

### When NOT to use
- Small collections — parallelism overhead dominates.
- I/O-bound operations — use `async/await` pipelines.
- When result order must be preserved strictly (use `.AsOrdered()`, but expect slower performance).

---

## 2. Merge Options and Buffering

**Project:** `PLINQ-producer-consumer-and-buffer`

By default PLINQ uses **auto-buffering** — it sends results to the consumer in chunks as they become available. You can override this with `WithMergeOptions`.

```csharp
var evenNumbers = items
    .AsParallel()
    .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
    .Where(x =>
    {
        Console.WriteLine($"Processing {x}; Thread: {Thread.CurrentThread.ManagedThreadId}");
        return (x % 2 == 0);
    });

foreach (var item in evenNumbers)
{
    // The foreach only starts after ALL parallel work is done
    Console.WriteLine($"Result: {item}; Thread: {Thread.CurrentThread.ManagedThreadId}");
}
```

### Merge options

| Option | Behaviour | Best for |
|---|---|---|
| `AutoBuffered` (default) | Results delivered in chunks as available | General purpose |
| `NotBuffered` | Results forwarded to consumer immediately as each completes | Streaming, low-latency |
| `FullyBuffered` | Consumer waits until all parallel work finishes, then gets all results | When all results are needed before processing any; enables ordering |

### Producer-consumer pattern
`FullyBuffered` is essentially a producer-consumer model:
- **Producers**: the ThreadPool threads running the parallel query.
- **Consumer**: the `foreach` loop (or `.ForAll`) that iterates results.
- The buffer decouples their speeds — producers fill the buffer while the consumer processes at its own pace.

### When to use `FullyBuffered`
- When you need results in a specific order (combine with `.AsOrdered()`).
- When the consumer is much faster than producers and you want to avoid interleaving output.
- When the downstream action (e.g., writing to a file) must be single-threaded.

---

## 3. Exception Handling in PLINQ

**Project:** `PLINQ-exception-handling`

Like `Parallel.For`, PLINQ collects all exceptions from parallel workers and throws them as a single `AggregateException`.

```csharp
var items = Enumerable.Range(1, 20);

var evenNumbers = items
    .AsParallel()
    .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
    .Where(x =>
    {
        if (x == 5)  throw new InvalidOperationException("Intentional at 5");
        if (x == 20) throw new ArgumentNullException("Intentional at 20");
        return (x % 2 == 0);
    });

try
{
    evenNumbers.ForAll(item =>
    {
        Console.WriteLine($"Result: {item}; Thread: {Thread.CurrentThread.ManagedThreadId}");
    });
}
catch (AggregateException ex)
{
    ex.Handle(inner =>
    {
        Console.WriteLine(inner.Message);
        return true;    // mark as handled — prevents re-throw
    });
}
```

### `AggregateException.Handle(predicate)`

`.Handle` iterates `InnerExceptions` and calls the predicate for each one. If the predicate returns `true` the exception is considered handled; if any returns `false`, a new `AggregateException` is thrown containing only the unhandled ones.

### Where exceptions can originate
1. Inside the `.Where` / `.Select` delegate.
2. Inside the `.ForAll` consumer action.
3. During result merging (rare).

All are collected and surfaced as one `AggregateException`.

### When to use
- Any PLINQ query where individual items might fail (e.g., parsing untrusted data, calling external services).
- Always wrap PLINQ queries in `try/catch (AggregateException)` in production.

---

## PLINQ vs Alternatives

| | Sequential LINQ | PLINQ | `Parallel.ForEach` |
|---|---|---|---|
| Syntax | LINQ query / method chain | LINQ + `.AsParallel()` | Explicit loop |
| Order preserved | Yes | Optional (`.AsOrdered()`) | No |
| Composability | High | High | Low |
| Custom partitioning | No | Via `Partitioner` | Via `Partitioner` |
| Best for | Small data, any op | CPU-bound LINQ pipelines | CPU-bound loops with side effects |

---

## PLINQ Decision Guide

```
Already using LINQ syntax?
├── No   →  Consider Parallel.ForEach or Task.Run
└── Yes
    ├── Collection large enough? (>1000 items with non-trivial predicate)
    │   ├── No   →  Keep sequential LINQ
    │   └── Yes  →  Add .AsParallel()
    ├── Need results in order?  →  Add .AsOrdered()
    ├── Need all results before consuming?  →  .WithMergeOptions(FullyBuffered)
    ├── Need low latency per result?  →  .WithMergeOptions(NotBuffered)
    └── Might throw exceptions?  →  Wrap in try/catch (AggregateException)
```
