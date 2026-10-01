using System.Collections.Concurrent;

ConcurrentQueue<string?> requestQueue = new ConcurrentQueue<string?>();

BlockingCollection<string?> collection = new BlockingCollection<string?>(requestQueue, 3);

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
        collection.CompleteAdding();
        break;
    }

    collection.Add(input);

    Console.WriteLine($"Enqueued: {input}; queue size: {collection.Count}");
}

void MonitorQueue()
{

    foreach (var request in collection.GetConsumingEnumerable())
    {
        if (collection.IsCompleted) break;

        Thread processingThread = new Thread(() => ProcessInput(request));
        processingThread.Start();

        Thread.Sleep(2000);
    }
}

// 3. Processing the requests
void ProcessInput(string? input)
{
    // Simulate processing time
    Thread.Sleep(2000);
    Console.WriteLine($"Processed input: {input}");
}
