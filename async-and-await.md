# Async and Await in C#

## What is `async`/`await`?

`async` and `await` are compiler-level syntactic sugar built on top of `Task`. They let you write **asynchronous code that reads like synchronous code** — no callbacks, no `.ContinueWith`, no blocking.

- **`async`** — marks a method as asynchronous. The compiler rewrites the method into a state machine.
- **`await`** — suspends execution of the current method **without blocking the thread**, then resumes after the awaited operation completes.

---

## 1. Basic Syntax

**Project:** `Basic-syntax-of-Async-and-Await`

```csharp
static async Task Main(string[] args)
{
    Console.WriteLine("Starting to do work.");
    var data = await FetchDataAsync();          // suspends here, releases the thread
    Console.WriteLine($"Data is fetched: {data}");
}

static async Task<string> FetchDataAsync()
{
    await Task.Delay(2000);     // non-blocking 2-second pause
    return "Complex data";
}
```

### Return types

| Return type | Use when |
|---|---|
| `async Task` | Method does work but returns nothing |
| `async Task<T>` | Method returns a value |
| `async void` | Only for event handlers — avoid elsewhere |
| `async ValueTask<T>` | Hot path where the result is often synchronous (avoids `Task` allocation) |

### What `await Task.Delay(2000)` does vs `Thread.Sleep(2000)`

| | `Thread.Sleep` | `await Task.Delay` |
|---|---|---|
| Blocks the thread | Yes | No |
| Thread is free during wait | No | Yes — can serve other work |
| Works in async context | Works (bad practice) | Correct |

### When to use
- Any I/O-bound operation: HTTP requests, file reads, database queries.
- Anywhere you previously used `.Result` or `.Wait()` and were willing to block a thread.

---

## 2. Overview — Real HTTP Call

**Project:** `Overview-Async-&-Await`

```csharp
OutputFirstPokemon();                   // returns immediately (fire-and-forget)

Console.WriteLine("This is the end of the program.");
Console.ReadLine();

async void OutputFirstPokemon()
{
    using var client = new HttpClient();
    var taskGetPokemonList = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

    var response = await taskGetPokemonList;    // suspend until HTTP completes

    var doc = JsonDocument.Parse(response);
    var firstPokemon = doc.RootElement.GetProperty("results")[0];

    Console.WriteLine($"First pokemon: {firstPokemon.GetProperty("name")}");
}
```

### Output order
```
This is the end of the program.   ← prints immediately
First pokemon: bulbasaur           ← prints after HTTP call finishes
```

The `"end of program"` line prints **before** the Pokemon name because `OutputFirstPokemon()` returns to the caller at the first `await`, and the caller continues.

### Why `async void` here?
This is a top-level program with no `async Main`. `async void` is used only here to demonstrate the concept — in production code, always return `Task` from `async` methods so exceptions are observable and the caller can `await` or inspect the result.

---

## 3. Which Thread Resumes After `await`?

**Project:** `which-thread-is-used`

This is one of the most important — and most misunderstood — aspects of `async`/`await`.

```csharp
static async Task Main(string[] args)
{
    Console.WriteLine($"1. Thread: {Thread.CurrentThread.ManagedThreadId}");

    var data = await FetchDataAsync();

    Console.WriteLine($"2. Thread: {Thread.CurrentThread.ManagedThreadId}");
}

static async Task<string> FetchDataAsync()
{
    Console.WriteLine($"3. Thread: {Thread.CurrentThread.ManagedThreadId}");
    await Task.Delay(2000);
    Console.WriteLine($"4. Thread: {Thread.CurrentThread.ManagedThreadId}");
    return "Complex data";
}
```

### Typical output (console app)

```
1. Thread: 1    ← Main thread
3. Thread: 1    ← FetchDataAsync starts on the main thread
4. Thread: 4    ← After Task.Delay, resumes on a ThreadPool thread
2. Thread: 4    ← Main continues on the SAME ThreadPool thread as step 4
```

### The rule
- In a **console app**, after `await` the continuation runs on **any available ThreadPool thread** (the `SynchronizationContext` is null).
- In a **WPF/WinForms app**, after `await` the continuation is posted back to the **UI thread** via the `SynchronizationContext`, so it is safe to update UI controls.
- In **ASP.NET Core**, there is no captured context — continuations run on ThreadPool threads.

### When to use `ConfigureAwait(false)`
```csharp
await SomeAsync().ConfigureAwait(false);
```
Tells the `await` machinery **not** to capture and restore the current `SynchronizationContext`. Use this in library code to avoid deadlocks when the library is consumed from a UI framework that has a synchronization context.

---

## 4. Chaining Awaited Calls

**Project:** `continuation-after-returning-value`

Using `await` instead of `.ContinueWith` + `.Unwrap()` makes chained async calls read sequentially.

```csharp
// Fetch the Pokemon list
var pokemonListJson = await client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

// Parse and extract the first Pokemon's URL
var doc = JsonDocument.Parse(pokemonListJson);
var url = doc.RootElement
             .GetProperty("results")[0]
             .GetProperty("url")
             .ToString();

// Fetch details using the extracted URL
var firstPokemonJson = await client.GetStringAsync(url);

// Parse and print details
doc = JsonDocument.Parse(firstPokemonJson);
var root = doc.RootElement;
Console.WriteLine($"Name:   {root.GetProperty("name")}");
Console.WriteLine($"Weight: {root.GetProperty("weight")}");
Console.WriteLine($"Height: {root.GetProperty("height")}");
```

Compare this to the equivalent `ContinueWith` chain in `TaskConinuation-Chain&Unwrap` — the `await` version is significantly easier to read, reason about, and debug.

### When to use
- Sequential async operations where each step depends on the result of the previous one.

---

## 5. `SynchronizationContext` in WPF / Windows Forms

**Project:** `Await-and-Synchronization-Context(Windows-Forms)`

In desktop apps, UI controls can only be modified on the UI thread. `await` automatically captures the `SynchronizationContext` before suspending and **posts the continuation back to the UI thread** when it resumes.

```csharp
// WPF button click handler
private async void Button_Click(object sender, RoutedEventArgs e)
{
    StatusLabel.Text = "Loading...";            // UI thread — safe

    var data = await FetchDataAsync();          // suspends; UI stays responsive

    StatusLabel.Text = $"Done: {data}";         // resumes on UI thread — safe
}
```

Without `async`/`await`, you would need `Dispatcher.Invoke(...)` to marshal back to the UI thread manually.

### Rules for desktop apps
- `async void` event handlers are the correct pattern for UI event handlers.
- Every `await` inside an event handler automatically returns to the UI thread.
- Use `ConfigureAwait(false)` in library methods that do not need to touch UI.

---

## `async`/`await` vs Alternatives

| Pattern | Readability | Thread-safe | Error handling | Best for |
|---|---|---|---|---|
| `async`/`await` | Excellent | Yes | `try/catch` works normally | All new I/O-bound code |
| `.ContinueWith` | Fair | Yes | `t.IsFaulted` check | Legacy code, fine-grained scheduling |
| `.Result` / `.Wait()` | Good | Yes | `AggregateException` | Sync entry points only |
| `Thread` + `Join` | Verbose | Manual | Manual collection | CPU-bound, custom priority |

---

## Common Mistakes

| Mistake | Consequence | Fix |
|---|---|---|
| `async void` outside event handler | Exceptions are unobservable | Return `async Task` |
| `.Result` inside `async` method | Deadlock on UI / ASP.NET | `await` instead |
| `await` inside a `lock` | Compile error | Use `SemaphoreSlim.WaitAsync()` |
| Forgetting `await` on a `Task` | Method returns before operation finishes | Always `await` or chain `.ContinueWith` |
| Not passing `CancellationToken` | No way to cancel | Thread tokens through every async call |
