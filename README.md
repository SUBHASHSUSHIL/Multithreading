# C# Multithreading & Concurrency — Learning Repository

A hands-on collection of C# projects that walk through every major concurrency concept in .NET — from raw `Thread` objects all the way up to PLINQ and the `async`/`await` pattern.

Each folder is a self-contained console application (targeting .NET 10) that demonstrates one focused concept. Read the companion markdown files for in-depth explanations of each topic area.

---

## Topic Map

| Markdown file | What it covers |
|---|---|
| [multithreading.md](multithreading.md) | Thread basics, lifecycle, priorities, cancellation, exception handling, thread-local storage, divide-and-conquer |
| [threads-synchronization.md](threads-synchronization.md) | Race conditions, `lock`, `Monitor`, `Mutex`, `Semaphore`, `ReaderWriterLockSlim`, deadlocks |
| [task-based-asynchronous-programming.md](task-based-asynchronous-programming.md) | `Task`, `Task<T>`, continuations, `WhenAll`, cancellation, exception handling |
| [async-and-await.md](async-and-await.md) | `async`/`await` syntax, `SynchronizationContext`, which thread resumes after `await` |
| [parallel-loops.md](parallel-loops.md) | `Parallel.For`, `Parallel.ForEach`, `Parallel.Invoke`, break/stop/cancel/exceptions |
| [pLINQ.md](pLINQ.md) | PLINQ basics, `ForAll`, merge options, buffering, exception handling |
| [concurrentQueue.md](concurrentQueue.md) | `ConcurrentQueue<T>`, `BlockingCollection<T>`, producer-consumer pattern |
| [multithreading-MISC.md](multithreading-MISC.md) | `AutoResetEvent`, `ManualResetEvent`, two-way signaling, `ThreadPool`, web-server simulation |

---

## Project Index

### Thread Fundamentals
| Project | Concept |
|---|---|
| `Basic-Syntax` | Creating threads, naming, priorities |
| `States-of-a-thread` | `ThreadState` lifecycle |
| `CancelingThread` | Cooperative cancellation with a `bool` flag |
| `Thread-Exception-Handing` | Catching exceptions thrown on worker threads |
| `DivideAndConquer` | Splitting work across threads, `Join`, measuring speedup |
| `thread-local-storage` | Per-thread accumulation in `Parallel.For` |
| `Web-Server-Simulation` | Queue-based request dispatcher with raw threads |

### Thread Synchronization
| Project | Concept |
|---|---|
| `Thread-Synchronization-Overview` | The race condition problem |
| `Threads-Synchronization-Overview` | Same demonstration, alternate project |
| `Exclusive-Lock` | `lock` keyword |
| `Moniter` | `Monitor.Enter/Exit` and `Monitor.TryEnter` with timeout |
| `Mutex` | Cross-process mutex protecting a shared file |
| `Semaphore` | `SemaphoreSlim` — limiting concurrency |
| `ReaderWriterLock` | `ReaderWriterLockSlim` — concurrent reads, exclusive writes |
| `Deadlock` | Intentional deadlock demonstration with two locks |
| `Airplane-Seats-Booking-System` | Real-world `lock` in a booking system |

### Task-Based Asynchronous Programming (TAP)
| Project | Concept |
|---|---|
| `OffloadTask` | Offloading CPU work with `Task.Run` |
| `ReturningResultsFromTask` | `Task<T>` — returning values from parallel segments |
| `TaskContinuation-ContinueWith` | Attaching a continuation with `.ContinueWith` |
| `TaskConinuation-Chain&Unwrap` | Chaining continuations, `.Unwrap()` for nested tasks |
| `TaskContinuation-Wait-WaitAll-Result` | Blocking with `.Wait()`, `.Result`, `Task.WaitAll` |
| `TaskContinuation-WhenAll` | Non-blocking aggregation with `Task.WhenAll` |
| `TaskSynchronization` | `SemaphoreSlim` applied to a `Task`-based server |
| `TaskCancelation` | `CancellationTokenSource`, `ThrowIfCancellationRequested`, `CancelAfter` |
| `ExceptionHandlingInTasks` | `AggregateException`, stored exceptions, `.IsFaulted` |

### Async & Await
| Project | Concept |
|---|---|
| `Basic-syntax-of-Async-and-Await` | `async Task`, `await`, `Task.Delay` |
| `Overview-Async-&-Await` | Real HTTP call with `async void` entry point |
| `which-thread-is-used` | Thread ID logging around `await` |
| `continuation-after-returning-value` | `await` chained HTTP calls |
| `Await-and-Synchronization-Context(Windows-Forms)` | `SynchronizationContext` in a WPF app |

### Parallel Loops
| Project | Concept |
|---|---|
| `parallel-loops-overview` | `Parallel.For`, `Parallel.ForEach`, `Parallel.Invoke` |
| `parallel-loops-break` | `state.Break()` and `LowestBreakIteration` |
| `parallel-loops-stop` | `state.Stop()` — unconditional halt |
| `parallel-loops-cancelation` | `ParallelOptions.CancellationToken` |
| `parallel-loops-exceptions` | `AggregateException` from parallel loops |
| `thread-local-storage` | Thread-local accumulator pattern |

### PLINQ
| Project | Concept |
|---|---|
| `basic-of-PLINQ` | `.AsParallel()`, `.Where()`, `.ForAll()` |
| `PLINQ-producer-consumer-and-buffer` | `WithMergeOptions` — `FullyBuffered` |
| `PLINQ-exception-handling` | `AggregateException` in PLINQ, `.Handle()` |

### Signaling & Collections
| Project | Concept |
|---|---|
| `AutoResetEvent` | One-at-a-time signaling |
| `ManualResetEvent` | Broadcast signal — release all waiting threads |
| `Two-way-signaling-in-producer-consumer-scenario` | Producer/consumer handshake with two `ManualResetEventSlim` |
| `ConcurrentQueue` | `ConcurrentQueue<T>` thread-safe queue |
| `BlockingCollection` | `BlockingCollection<T>` — bounded queue, back-pressure, `CompleteAdding` |
| `ThreadPool` | `ThreadPool.QueueUserWorkItem` |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any C# IDE (Visual Studio 2022+, Rider, or VS Code with the C# extension)

## Running a project

```bash
cd <project-folder>
dotnet run
```
