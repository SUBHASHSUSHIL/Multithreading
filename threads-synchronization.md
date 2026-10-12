# Thread Synchronization in C#

## Why Synchronization?

When two or more threads read and write the **same variable** without coordination, the result is a **race condition** — the final value depends on the unpredictable order of execution. This is the core problem that all synchronization primitives solve.

**Project:** `Thread-Synchronization-Overview`

```csharp
int counter = 0;

Thread thread1 = new Thread(IncrementCounter);
Thread thread2 = new Thread(IncrementCounter);
thread1.Start();
thread2.Start();
thread1.Join();
thread2.Join();

// Expected: 200 000. Actual: something less — data is lost.
Console.WriteLine(counter);

void IncrementCounter()
{
    for (int i = 0; i < 100000; i++)
        counter = counter + 1;  // read-modify-write is NOT atomic
}
```

`counter = counter + 1` compiles to three CPU instructions (read, add, write). Two threads can interleave those instructions and silently discard each other's increments.

---

## 1. `lock` — Exclusive Lock

**Project:** `Exclusive-Lock`

The `lock` statement is the simplest way to protect a critical section. Only one thread can hold the lock at a time; all others block until it is released.

```csharp
int counter = 0;
object counterLock = new object();

void IncrementCounter()
{
    for (int i = 0; i < 100000; i++)
    {
        lock (counterLock)
        {
            counter = counter + 1;  // safe: only one thread here at a time
        }
    }
}
```

### Rules
- Always lock on a **private `object`** — never on `this`, `typeof(T)`, or a string literal.
- Keep the protected section **as short as possible**. Long critical sections serialize threads and destroy parallelism.
- `lock` is re-entrant: a thread that already holds the lock can enter the same lock again without deadlocking.

### When to use
- Protecting a simple shared variable or a short block of code inside a single process.

---

## 2. `Monitor` — Lock with Timeout

**Project:** `Moniter`

`Monitor` is what the `lock` keyword compiles to. Using it directly gives you `Monitor.TryEnter`, which lets you **attempt** to acquire the lock and give up after a timeout instead of blocking forever.

```csharp
// Always use try/finally to guarantee the lock is released
Monitor.Enter(counterLock);
try
{
    counter = counter + 1;
}
finally
{
    Monitor.Exit(counterLock);
}
```

```csharp
// TryEnter — non-blocking attempt with a 2-second timeout
if (Monitor.TryEnter(ticketsLock, 2000))
{
    try
    {
        // process booking
    }
    finally
    {
        Monitor.Exit(ticketsLock);
    }
}
else
{
    Console.WriteLine("The system is busy. Please wait.");
}
```

### `Monitor.TryEnter` vs `lock`

| | `lock` | `Monitor.TryEnter` |
|---|---|---|
| Blocks forever | Yes | No — returns `false` after timeout |
| Use case | Normal critical sections | When a timeout/fallback is needed |

### When to use
- When you need to detect contention and report a "busy" message to the user instead of stalling the request.
- Booking systems, rate limiters, or any scenario where a request should fail fast rather than queue indefinitely.

---

## 3. `Mutex` — Cross-Process Synchronization

**Project:** `Mutex`

A `Mutex` works like `lock` but is visible across **process boundaries** via a global name. Use it when two separate running applications must not access the same resource simultaneously.

```csharp
using (var mutex = new Mutex(false, $"GlobalFileMutex:{filePath}"))
{
    for (int i = 0; i < 10000; i++)
    {
        mutex.WaitOne();    // acquire
        try
        {
            int counter = ReadCounter(filePath);
            counter++;
            WriteCounter(filePath, counter);
        }
        finally
        {
            mutex.ReleaseMutex();   // always release in finally
        }
    }
}
```

### `Mutex` vs `lock`

| | `lock` | `Mutex` |
|---|---|---|
| Scope | Single process | Cross-process |
| Performance | Fast (kernel-free fast path) | Slower (kernel object) |
| Named? | No | Yes (`"GlobalFileMutex:..."`) |

### When to use
- Protecting a shared file, database, or resource that multiple application instances may access at the same time.
- Single-instance application enforcement.

---

## 4. `SemaphoreSlim` — Limit Concurrent Access

**Project:** `Semaphore`

A semaphore holds a **count**. Each `Wait()` decrements the count; each `Release()` increments it. When the count reaches zero, further `Wait()` calls block. This lets you allow up to N threads into a section simultaneously.

```csharp
using SemaphoreSlim semaphore = new SemaphoreSlim(initialCount: 3, maxCount: 3);

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            var input = requestQueue.Dequeue();
            semaphore.Wait();               // blocks when 3 threads are already inside
            new Thread(() => ProcessInput(input)).Start();
        }
    }
}

void ProcessInput(string? input)
{
    try
    {
        Thread.Sleep(2000);
        Console.WriteLine($"Processed: {input}");
    }
    finally
    {
        semaphore.Release();    // always release in finally
    }
}
```

### `SemaphoreSlim` vs `Semaphore`

| | `SemaphoreSlim` | `Semaphore` |
|---|---|---|
| Cross-process | No | Yes |
| Performance | Faster | Slower |
| `async` support | Yes (`.WaitAsync()`) | No |

### When to use
- Throttling outbound HTTP requests to an external API that has a rate limit.
- Limiting concurrent database connections.
- Controlling concurrency in a request queue dispatcher.

---

## 5. `ReaderWriterLockSlim` — Concurrent Reads, Exclusive Writes

**Project:** `ReaderWriterLock`

A plain `lock` serializes all access — even reads that could safely run in parallel. `ReaderWriterLockSlim` allows **multiple concurrent readers** but only one writer at a time.

```csharp
public class GlobalConfigurationCache
{
    private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private Dictionary<int, string> _cache = new Dictionary<int, string>();

    public void Add(int key, string value)
    {
        _lock.EnterWriteLock();
        try   { _cache[key] = value; }
        finally { _lock.ExitWriteLock(); }
    }

    public string? Get(int key)
    {
        _lock.EnterReadLock();
        try   { return _cache.TryGetValue(key, out var v) ? v : null; }
        finally { _lock.ExitReadLock(); }
    }
}
```

### Access modes

| Mode | Allows others? | Use for |
|---|---|---|
| Read | Other readers — yes | Any read operation |
| Write | Nobody | Any write operation |
| Upgradeable Read | Other readers — yes; writers — no | "Check then write" patterns |

### When to use
- Caches, configuration stores, or lookup tables that are read far more often than they are written.
- Replace a plain `lock` with `ReaderWriterLockSlim` when profiling shows reader contention.

---

## 6. Deadlock

**Project:** `Deadlock`

A deadlock occurs when two threads each hold a lock the other needs, and both wait forever.

```csharp
object userLock  = new object();
object orderLock = new object();

void ManageUser()           // Thread 1
{
    lock (userLock)         // acquires user lock
    {
        Thread.Sleep(2000);
        lock (orderLock)    // waits for order lock — blocked by Thread 2
        { ... }
    }
}

void ManageOrder()          // Thread 2
{
    lock (orderLock)        // acquires order lock
    {
        Thread.Sleep(1000);
        lock (userLock)     // waits for user lock — blocked by Thread 1
        { ... }
    }
}
```

### How to prevent deadlocks

1. **Consistent lock ordering** — always acquire locks in the same order across all threads.
2. **`Monitor.TryEnter` with timeout** — detect and back off rather than waiting indefinitely.
3. **Minimize lock scope** — hold locks for the shortest time possible.
4. **Use higher-level primitives** — `SemaphoreSlim`, `ConcurrentQueue`, or `Channel<T>` often eliminate the need for nested locks entirely.

---

## 7. Airplane Seats Booking System

**Project:** `Airplane-Seats-Booking-System`

A realistic example combining a request queue, worker threads, and `lock` to safely manage shared ticket inventory.

```csharp
int availableTickets = 10;
object ticketsLock = new object();

void ProcessBooking(string? input)
{
    Thread.Sleep(2000);  // simulate processing

    lock (ticketsLock)
    {
        if (input == "b")
        {
            if (availableTickets > 0)
            {
                availableTickets--;
                Console.WriteLine($"Booked. {availableTickets} remaining.");
            }
        }
        else if (input == "c")
        {
            if (availableTickets < 10)
            {
                availableTickets++;
                Console.WriteLine($"Cancelled. {availableTickets} available.");
            }
        }
    }
}
```

The `lock` ensures that even if multiple booking requests are processed simultaneously by different threads, the `availableTickets` counter is never read and written at the same time, so no seats are double-booked or lost.

---

## Synchronization Primitives — Quick Comparison

| Primitive | Scope | Max concurrent | Key method | Best for |
|---|---|---|---|---|
| `lock` | In-process | 1 | `lock(obj){}` | Simple exclusive access |
| `Monitor` | In-process | 1 | `TryEnter(obj, ms)` | Lock + timeout |
| `Mutex` | Cross-process | 1 | `WaitOne` / `ReleaseMutex` | Named, system-wide lock |
| `SemaphoreSlim` | In-process | N | `Wait` / `Release` | Throttling concurrency |
| `ReaderWriterLockSlim` | In-process | Many readers / 1 writer | `EnterReadLock` / `EnterWriteLock` | Read-heavy caches |
