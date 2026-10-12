# Multithreading in C#

## What is a Thread?

A **thread** is the smallest unit of execution inside a process. Every .NET program starts with one thread — the **main thread** — and you can create additional threads to run work concurrently on multiple CPU cores.

Use raw threads when you need precise control over priority, naming, or lifetime and when none of the higher-level abstractions (`Task`, `Parallel`, `async/await`) are a good fit. For most modern code, prefer `Task`-based APIs instead.

---

## 1. Creating and Starting Threads

**Project:** `Basic-Syntax`

```csharp
Thread thread1 = new Thread(WriteThreadId);
Thread thread2 = new Thread(WriteThreadId);

thread1.Name = "Thread 1";
thread2.Name = "Thread 2";
Thread.CurrentThread.Name = "Main Thread";

thread1.Priority = ThreadPriority.Highest;
thread2.Priority = ThreadPriority.Lowest;

thread1.Start();
thread2.Start();
```

### Key concepts

| Concept | Detail |
|---|---|
| `new Thread(method)` | Creates a thread; the thread is **not** running yet |
| `.Start()` | Schedules the thread on the OS; it may not execute immediately |
| `.Name` | Optional label — shows up in debuggers and logs |
| `Thread.CurrentThread` | Returns the calling thread's object |
| `ThreadPriority` | Hint to the OS scheduler (`Lowest` → `Highest`). The OS is free to ignore it. |

### When to use
- You need to name a thread for debugging.
- You want to set a custom priority (rare in practice).
- You are learning how the threading model works before using higher-level APIs.

---

## 2. Thread State Lifecycle

**Project:** `States-of-a-thread`

A thread moves through well-defined states during its lifetime:

```
Unstarted → Running → WaitSleepJoin / Background → Stopped
```

```csharp
Console.WriteLine(thread.ThreadState);  // "Unstarted"
thread.Start();
Console.WriteLine(thread.ThreadState);  // "Running"
Thread.Sleep(3000);                     // During sleep: "WaitSleepJoin"
// After work(): "Stopped"
```

### States reference

| State | Meaning |
|---|---|
| `Unstarted` | Thread created, `Start()` not called yet |
| `Running` | Actively executing |
| `WaitSleepJoin` | Blocked by `Sleep`, `Join`, or a wait handle |
| `Background` | Marked as background — process can exit even if it is still running |
| `Stopped` | Method returned; thread is done |

### When to use
- Monitoring or logging thread health.
- Debugging unexpected thread behaviour.

---

## 3. Cooperative Thread Cancellation

**Project:** `CancelingThread`

.NET threads cannot be forcibly aborted safely (`Thread.Abort` was removed in .NET 5+). Instead, use a **shared flag** that the worker checks periodically.

```csharp
bool cancelThread = false;

Thread thread = new Thread(Work);
thread.Start();

var input = Console.ReadLine();
if (input == "c") cancelThread = true;

thread.Join();   // Wait for the thread to finish cleanly

void Work()
{
    for (int i = 0; i < 100000; i++)
    {
        if (cancelThread)
        {
            Console.WriteLine($"Cancelled at iteration {i}");
            break;
        }
        Thread.SpinWait(300000);
    }
}
```

### Why cooperative?
Forcible termination leaves shared state in an inconsistent condition (open files, half-written data, unreleased locks). Cooperative cancellation lets the worker finish its current unit of work and clean up before stopping.

### When to use
- Long-running background workers that should respond to user input.
- For `Task`-based code, use `CancellationToken` instead (see `task-based-asynchronous-programming.md`).

---

## 4. Exception Handling on Worker Threads

**Project:** `Thread-Exception-Handing`

Exceptions thrown on a worker thread do **not** propagate to the thread that started it. You must catch them inside the worker and relay them manually.

```csharp
List<Exception> exceptions = new List<Exception>();
object lockExceptions = new object();

Thread thread1 = new Thread(Work);
Thread thread2 = new Thread(Work);
thread1.Start();
thread2.Start();
thread1.Join();
thread2.Join();

foreach (var ex in exceptions)
    Console.WriteLine(ex.Message);

void Work()
{
    try
    {
        throw new InvalidOperationException("An error occurred.");
    }
    catch (Exception ex)
    {
        lock (lockExceptions)  // List<T> is not thread-safe
        {
            exceptions.Add(ex);
        }
    }
}
```

### Pattern
1. Wrap all worker logic in `try/catch`.
2. Collect exceptions in a thread-safe way (a `lock`-protected list works; `ConcurrentBag` also works).
3. Process exceptions after `Join()` returns, back on the calling thread.

### When to use
- Any multi-threaded code where a worker could fail and the caller needs to know.
- For `Task`-based code, exceptions are captured automatically in `AggregateException`.

---

## 5. Divide-and-Conquer — Parallel Speedup

**Project:** `DivideAndConquer`

Split a large data set into equal segments, sum each segment on its own thread, and combine the partial results after all threads finish.

```csharp
int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
int sum1 = 0, sum2 = 0, sum3 = 0, sum4 = 0;

int numofThreads = 4;
int segmentLength = array.Length / numofThreads;

Thread[] threads = new Thread[numofThreads];
threads[0] = new Thread(() => { sum1 = SumSegment(0, segmentLength); });
// ... create the other 3 threads similarly

foreach (var t in threads) t.Start();
foreach (var t in threads) t.Join();

Console.WriteLine(sum1 + sum2 + sum3 + sum4);
```

### Why this works
Each thread works on its own segment — no shared mutable state during computation, so no synchronization is needed. The combine step runs only after all threads have finished (`Join`).

### When to use
- CPU-bound work on large arrays or data sets.
- When you want to benchmark single-threaded vs. multi-threaded execution time.
- Use `Task<T>` and `ReturningResultsFromTask` for cleaner code in production.

---

## 6. Thread-Local Storage in Parallel Loops

**Project:** `thread-local-storage`

Each parallel iteration runs on a thread-pool thread. Instead of acquiring a shared lock on every iteration (expensive), give each thread its own local accumulator and only merge once at the end.

```csharp
int sum = 0;
object lockSum = new object();

Parallel.For(
    0,
    array.Length,
    () => 0,                        // thread-local initial value
    (i, state, tls) =>
    {
        tls += array[i];            // accumulate locally — no lock needed
        return tls;
    },
    tls =>
    {
        lock (lockSum)              // merge once per thread at the end
        {
            sum += tls;
        }
    }
);
```

### When to use
- Aggregation inside `Parallel.For` / `Parallel.ForEach` where locking every iteration would create a bottleneck.
- The pattern pays off when the number of iterations is much larger than the number of threads.

---

## 7. Web-Server Request Dispatcher

**Project:** `Web-Server-Simulation`

A minimal pattern that underpins real web servers: a main thread enqueues requests while a dedicated monitoring thread dequeues them and spawns a worker thread per request.

```csharp
Queue<string?> requestQueue = new Queue<string?>();

Thread monitoringThread = new Thread(MonitorQueue);
monitoringThread.Start();

while (true)                        // main thread — accepts input
{
    var input = Console.ReadLine();
    if (input?.ToLower() == "exit") break;
    requestQueue.Enqueue(input);
}

void MonitorQueue()                 // runs on a dedicated thread
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            var input = requestQueue.Dequeue();
            new Thread(() => ProcessInput(input)).Start();
        }
        Thread.Sleep(100);
    }
}
```

> **Note:** The plain `Queue<T>` used here is not thread-safe. See `concurrentQueue.md` for the production-ready version using `ConcurrentQueue<T>`.

### When to use
- As a teaching model for the producer-consumer pattern.
- Real applications should use `ConcurrentQueue<T>`, `BlockingCollection<T>`, or `Channel<T>`.

---

## Thread vs. Task — Quick Reference

| | `Thread` | `Task` |
|---|---|---|
| Abstraction level | Low | High |
| Thread pool | No (always creates a new OS thread) | Yes (`Task.Run` uses the pool) |
| Return value | Not directly | `Task<T>` |
| Cancellation | Manual `bool` flag | `CancellationToken` |
| Exception propagation | Manual collection | `AggregateException` |
| Best for | Custom priority/name, background daemon threads | General async/parallel work |
