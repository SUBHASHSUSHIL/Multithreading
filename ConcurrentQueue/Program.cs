using System.Collections.Concurrent;

ConcurrentQueue<string?> requestQueue = new ConcurrentQueue<string?>();

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

    requestQueue.Enqueue(input);
}

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            if (requestQueue.TryDequeue(out var input))
            {
                Thread processingThread = new Thread(() => ProcessInput(input));
                processingThread.Start();
            }
        }
        Thread.Sleep(100);
    }
}

// 3. Processing the requests
void ProcessInput(string? input)
{
    // Simulate processing time
    Thread.Sleep(2000);
    Console.WriteLine($"Processed input: {input}");
}
