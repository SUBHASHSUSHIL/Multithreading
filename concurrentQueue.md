# Concurrent Collections in C#

## The Problem with `Queue<T>` in Multi-Threaded Code

The standard `Queue<T>` is not thread-safe. If a producer enqueues items while a consumer dequeues them simultaneously — without external synchronization — you get corrupted state, lost items, or exceptions.

The `System.Collections.Concurrent` namespace provides thread-safe collections that handle this internally, with far better performance than wrapping a plain collection in a `lock`.

---

## 1. `ConcurrentQueue<T>`

**Project:** `ConcurrentQueue`

`ConcurrentQueue<T>` is a thread-safe, lock-free FIFO queue. Any number of producers and consumers can enqueue and dequeue simultaneously without external locking.

```csharp
ConcurrentQueue<string?> requestQueue = new ConcurrentQueue<string?>();

// Producer — main thread
while (true)
{
    string? input = Console.ReadLine();
    if (input?.ToLower() == "exit") break;
    requestQueue.Enqueue(input);                    // thread-safe
}

// Consumer — monitoring thread
void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            if (requestQueue.TryDequeue(out var input)) // thread-safe
            {
                new Thread(() => ProcessInput(input)).Start();
            }
        }
        Thread.Sleep(100);
    }
}
```

### Key API

| Method | Description |
|---|---|
| `.Enqueue(item)` | Add an item to the tail — always succeeds |
| `.TryDequeue(out item)` | Remove from head — returns `false` if empty |
| `.TryPeek(out item)` | Inspect head without removing — returns `false` if empty |
| `.Count` | Approximate count (exact between operations) |
| `.IsEmpty` | `true` when no items are present |

### Why `TryDequeue` instead of `Dequeue`?
`ConcurrentQueue<T>` has no `Dequeue()` method because the queue could be empty by the time the call executes (another thread could have removed the last item). `TryDequeue` returns `false` gracefully instead of throwing.

### When to use
- Producer-consumer patterns where producers and consumers run on different threads.
- Request dispatchers, job queues, event pipelines.
- Replacing `lock`-wrapped `Queue<T>` for better throughput.

---

## 2. `BlockingCollection<T>`

**Project:** `BlockingCollection`

`BlockingCollection<T>` wraps any `IProducerConsumerCollection<T>` (typically `ConcurrentQueue<T>`) and adds two powerful features:

1. **Bounded capacity** — blocks producers when the queue is full (back-pressure).
2. **Completion signaling** — producers call `CompleteAdding()` and consumers drain gracefully.

```csharp
ConcurrentQueue<string?> requestQueue = new ConcurrentQueue<string?>();

// Bounded to 3 items — producer blocks when 3 items are already waiting
BlockingCollection<string?> collection = new BlockingCollection<string?>(requestQueue, 3);

// Producer — main thread
while (true)
{
    string? input = Console.ReadLine();
    if (input?.ToLower() == "exit")
    {
        collection.CompleteAdding();    // signal: no more items coming
        break;
    }

    collection.Add(input);              // blocks if queue is at capacity (3 items)
    Console.WriteLine($"Queued: {input}; size: {collection.Count}");
}

// Consumer — monitoring thread
void MonitorQueue()
{
    // GetConsumingEnumerable blocks when empty, exits when CompleteAdding is called
    foreach (var request in collection.GetConsumingEnumerable())
    {
        if (collection.IsCompleted) break;
        new Thread(() => ProcessInput(request)).Start();
        Thread.Sleep(2000);             // simulate consuming at a slower rate
    }
}
```

### Key API

| Method / Property | Description |
|---|---|
| `.Add(item)` | Adds an item; **blocks** if at bounded capacity |
| `.TryAdd(item, timeout)` | Non-blocking add with optional timeout |
| `.Take()` | Removes an item; **blocks** if empty |
| `.TryTake(out item, timeout)` | Non-blocking take with optional timeout |
| `.GetConsumingEnumerable()` | `IEnumerable` that blocks when empty, ends when `CompleteAdding()` is called |
| `.CompleteAdding()` | Marks collection as complete — no more `Add` calls allowed |
| `.IsAddingCompleted` | `true` after `CompleteAdding()` |
| `.IsCompleted` | `true` when adding is complete AND collection is empty |
| `.BoundedCapacity` | The max item count (-1 if unbounded) |
| `.Count` | Current item count |

### Bounded capacity — back-pressure
When `collection.Add(item)` is called and `Count == BoundedCapacity`, the producer **blocks** until a consumer removes an item. This is the classic **back-pressure** mechanism — it prevents the producer from overwhelming the consumer and exhausting memory.

### `GetConsumingEnumerable` — clean consumer loop
```csharp
foreach (var item in collection.GetConsumingEnumerable())
{
    Process(item);  // blocks when empty, automatically stops when Complete + empty
}
```
This is the idiomatic consumer pattern. It replaces a `while(true)` loop with sleeps.

### When to use `BlockingCollection<T>`
- When you need **bounded capacity** to apply back-pressure on producers.
- When producers need to **signal completion** and consumers need to drain cleanly.
- Pipeline architectures where stages run at different speeds.
- Replacing manual `ManualResetEvent`/`Monitor.Wait` signaling between producer and consumer.

---

## `ConcurrentQueue` vs `BlockingCollection` — Quick Comparison

| Feature | `ConcurrentQueue<T>` | `BlockingCollection<T>` |
|---|---|---|
| Thread-safe | Yes | Yes |
| Bounded capacity | No | Yes |
| Blocks producer when full | No | Yes |
| Blocks consumer when empty | No | Yes (`.Take()`, `.GetConsumingEnumerable()`) |
| Completion signal | No | Yes (`.CompleteAdding()`) |
| Underlying collection | Self (lock-free queue) | Any `IProducerConsumerCollection<T>` |
| `async` support | No | No — use `Channel<T>` for async |
| Best for | High-throughput, non-blocking dispatch | Bounded pipelines, graceful shutdown |

---

## `Channel<T>` — Modern Alternative

For fully `async/await`-compatible producer-consumer pipelines (no blocking threads), .NET provides `System.Threading.Channels.Channel<T>`:

```csharp
var channel = Channel.CreateBounded<string>(3);

// async producer
await channel.Writer.WriteAsync(item);

// async consumer
await foreach (var item in channel.Reader.ReadAllAsync())
    Process(item);
```

Consider `Channel<T>` when your producer and consumer are both `async` methods and you want to avoid blocking any threads.
