Queue<string?> requestQueue = new Queue<string?>();

object queueLock = new object();
using SemaphoreSlim semaphore = new SemaphoreSlim(initialCount: 3, maxCount: 3);

// 2. Start the requests queue monitoring thread
Thread monitoringThread = new Thread(MonitorQueue);
monitoringThread.Start();

// 1. Enqueue the requests
Console.WriteLine("Server is running. Type 'exit' to stop.");
while (true)
{
    string? input = Console.ReadLine();
    if (input?.ToLower() == "exit")
    {
        break;
    }

    lock (queueLock)
    {
        requestQueue.Enqueue(input);
    }

}

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            string? input;
            lock (queueLock)
            {
                input = requestQueue.Dequeue();
            }
            semaphore.Wait();
            Thread processingThread = new Thread(() => ProcessInput(input));
            processingThread.Start();
        }
        Thread.Sleep(100);
    }
}

// 3. Processing the requests
void ProcessInput(string? input)
{
    try
    {
        // Simulate processing time
        Thread.Sleep(2000);
        Console.WriteLine($"Processed input: {input}");
    }
    finally
    {
        var prevCount = semaphore.Release();
        Console.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId} released the semaphore. Previous count is: {prevCount}");
    }
}
