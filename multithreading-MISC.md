# Miscellaneous Threading — Signaling, ThreadPool & More

This file covers signaling primitives (`AutoResetEvent`, `ManualResetEvent`), the two-way producer-consumer handshake, and `ThreadPool` — topics that don't fit neatly into the other categories but are essential for real-world multithreaded applications.

---

## 1. `AutoResetEvent` — One-at-a-Time Signaling

**Project:** `AutoResetEvent`

An `AutoResetEvent` is a signaling primitive with two states: **Set** (signaled) and **Reset** (not signaled).

- Calling `.Set()` releases **exactly one** waiting thread, then automatically resets back to the non-signaled state.
- Every waiting thread calls `.WaitOne()`, which blocks until the event is set.

```csharp
using AutoResetEvent autoResetEvent = new AutoResetEvent(false); // starts non-signaled

// Start 3 worker threads — each waits for a signal before proceeding
for (int i = 0; i < 3; i++)
{
    Thread worker = new Thread(Worker);
    worker.Name = $"Worker {i + 1}";
    worker.Start();
}

// Main thread sends one signal at a time
while (true)
{
    string? input = Console.ReadLine();
    if (input?.ToLower() == "go")
        autoResetEvent.Set();   // releases exactly ONE waiting thread
}

void Worker()
{
    while (true)
    {
        Console.WriteLine($"{Thread.CurrentThread.Name} is waiting...");
        autoResetEvent.WaitOne();   // blocks until Set() is called
        Console.WriteLine($"{Thread.CurrentThread.Name} proceeds.");
        Thread.Sleep(2000);
    }
}
```

### Key behaviour — "auto" reset

Each call to `.Set()` releases **one** thread and the event immediately resets to non-signaled. The next call to `.Set()` releases the next waiting thread. This makes it ideal for a **work-item slot** pattern — each "go" signal corresponds to exactly one unit of work.

### When to use
- Controlling access to a resource one thread at a time.
- Passing a "proceed" token to a pool of workers, one worker per token.
- Pipeline stages where the upstream signals the downstream one item at a time.

### `AutoResetEvent` vs `ManualResetEvent`

| | `AutoResetEvent` | `ManualResetEvent` |
|---|---|---|
| Releases | One waiting thread | All waiting threads |
| Resets automatically | Yes | No — must call `.Reset()` manually |
| Analogy | Turnstile (one person at a time) | Gate (everyone passes at once) |

---

## 2. `ManualResetEventSlim` — Broadcast Signal

**Project:** `ManualResetEvent`

A `ManualResetEventSlim` starts in the **Reset** (blocked) state. Calling `.Set()` releases **all** currently waiting threads simultaneously. It stays set until you explicitly call `.Reset()`.

```csharp
using ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(false);

// Spawn 3 threads — all wait at the gate
for (int i = 1; i <= 3; i++)
{
    Thread thread = new Thread(Work);
    thread.Name = $"Thread {i}";
    thread.Start();
}

Console.ReadLine();         // press Enter to open the gate
manualResetEvent.Set();     // releases ALL 3 threads at once

void Work()
{
    Console.WriteLine($"{Thread.CurrentThread.Name} is waiting...");
    manualResetEvent.Wait();    // block until Set()
    Thread.Sleep(1000);
    Console.WriteLine($"{Thread.CurrentThread.Name} released.");
}
```

### Key behaviour — manual reset
The event **stays set** after `.Set()` is called. Any thread that arrives later and calls `.Wait()` passes through immediately. Call `.Reset()` to close the gate again.

### `ManualResetEventSlim` vs `ManualResetEvent`

| | `ManualResetEventSlim` | `ManualResetEvent` |
|---|---|---|
| Cross-process | No | Yes |
| Performance | Faster (spin-wait + light kernel wait) | Slower (always kernel) |
| `async` support | No | No |
| Use for | In-process broadcast | Cross-process or interop |

### When to use
- Starting a batch of pre-initialized threads simultaneously at a specific moment.
- "Ready, set, go" — all workers start when a starting gun fires.
- Implementing a phase barrier (reset the event to set up the next phase).

---

## 3. Two-Way Signaling — Producer-Consumer Handshake

**Project:** `Two-way-signaling-in-producer-consumer-scenario`

A more sophisticated pattern using **two** `ManualResetEventSlim` instances to coordinate a producer and multiple consumers in a round-trip cycle.

```
Producer                    Consumers
   │                            │
   │─── consumeEvent.Set() ────►│  (consumers process items)
   │                            │
   │◄── produceEvent.Set() ────-│  (all consumers done, request more)
   │                            │
   │  (producer enqueues more)  │
   │─── consumeEvent.Set() ────►│
   ...
```

```csharp
Queue<int> queue = new Queue<int>();

ManualResetEventSlim consumeEvent = new ManualResetEventSlim(false); // gate for consumers
ManualResetEventSlim produceEvent = new ManualResetEventSlim(true);  // gate for producer

int consumerCount = 0;
object lockConsumerCount = new object();

// Producer
while (true)
{
    produceEvent.Wait();        // wait until consumers signal "ready for more"
    produceEvent.Reset();

    // enqueue 10 items
    for (int i = 1; i <= 10; i++) queue.Enqueue(i);
    consumeEvent.Set();         // release consumers
}

// Consumer
void Consume()
{
    while (true)
    {
        consumeEvent.Wait();    // wait for items to be available

        while (queue.TryDequeue(out int item))
        {
            Thread.Sleep(500);
            Console.WriteLine($"Consumed {item} on {Thread.CurrentThread.Name}");
        }

        lock (lockConsumerCount)
        {
            consumerCount++;
            if (consumerCount == 3)     // all 3 consumers finished their batch
            {
                consumeEvent.Reset();   // close the consumer gate
                produceEvent.Set();     // signal producer to produce more
                consumerCount = 0;
            }
        }
    }
}
```

### Why two events?
- `consumeEvent` — controlled by the producer; tells consumers "there is work available".
- `produceEvent` — controlled by consumers; tells the producer "we are done, produce more".

This pattern prevents both overpopulation of the queue (no bound) and starvation (producer always waits until consumers are ready).

### When to use
- Strict batch-processing pipelines where the producer must wait for all consumers to finish a batch before producing the next one.
- Systems with a hard memory or throughput constraint on queue size.
- Use `BlockingCollection<T>` for simpler producer-consumer scenarios — this pattern is for cases that need explicit phase control.

---

## 4. `ThreadPool`

**Project:** `ThreadPool`

The `ThreadPool` maintains a pool of pre-created threads. Submitting work to the pool (`QueueUserWorkItem`) avoids the overhead of creating and destroying a new OS thread for each request.

```csharp
Queue<string?> requestQueue = new Queue<string?>();

Thread monitoringThread = new Thread(MonitorQueue);
monitoringThread.Start();

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            string? input = requestQueue.Dequeue();
            // Submit to thread pool instead of new Thread(...)
            ThreadPool.QueueUserWorkItem(ProcessInput, input);
        }
        Thread.Sleep(100);
    }
}

void ProcessInput(object? input)
{
    Thread.Sleep(2000);
    Console.WriteLine($"Processed: {input} | IsThreadPoolThread: {Thread.CurrentThread.IsThreadPoolThread}");
}
```

### `ThreadPool.QueueUserWorkItem` vs `new Thread(...)`

| | `new Thread` | `ThreadPool.QueueUserWorkItem` |
|---|---|---|
| Thread creation cost | High (new OS thread) | Low (reuses pooled thread) |
| Control over priority | Yes | No |
| Control over name | Yes | No |
| Background by default | No | Yes |
| Return value | No | No (use `Task` instead) |

### `ThreadPool` vs `Task.Run`

`Task.Run(...)` is built on top of `ThreadPool` and is the **preferred** API because:
- It returns a `Task` you can `await`, chain with `.ContinueWith`, or observe for exceptions.
- It integrates with `CancellationToken`.
- It supports return values via `Task<T>`.

Use `ThreadPool.QueueUserWorkItem` only when you need the lowest-overhead fire-and-forget dispatch and do not need any of the `Task` features.

### When to use `ThreadPool` directly
- High-frequency, very short-lived work items where `Task` allocation overhead is measurable.
- Legacy code or interop scenarios that predate TAP.
- When you just need fire-and-forget with no result, error handling, or cancellation.

---

## Signaling Primitives — Summary

| Primitive | Releases | Resets | Cross-process | Best for |
|---|---|---|---|---|
| `AutoResetEvent` | One thread | Automatically | Yes | Work tokens, one-at-a-time dispatch |
| `ManualResetEvent` | All threads | Manually | Yes | Broadcast, cross-process phase gate |
| `ManualResetEventSlim` | All threads | Manually | No | In-process broadcast (faster) |
| `CountdownEvent` | All when count reaches 0 | Manual reset | No | Fan-out / fan-in patterns |
| `Barrier` | All when all arrive | Automatic per phase | No | Iterative parallel algorithms |
| `SemaphoreSlim` | N threads | N/A | No | Throttling concurrency |
