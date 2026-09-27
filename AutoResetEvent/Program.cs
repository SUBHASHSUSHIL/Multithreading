///////////////////////////////////////////////////////////////////////
// Single Worker thread
/////////////////////////////////////////////////////////////////////// 
using AutoResetEvent autoResetEvent = new AutoResetEvent(false);
string? userInput = null;

Console.WriteLine("Server is running. Type 'go' to proceed and  'exit' to stop.");

// Start the worker thread
for (int i = 0; i < 3; i++)
{
    Thread workerThread = new Thread(Worker);
    workerThread.Name = $"Worker {i + 1}";
    workerThread.Start();
}

// Main thread receives user input

while (true)
{
    userInput = Console.ReadLine();

    // Signal the worker thread if input is "go"
    if (userInput.ToLower() == "go")
    {
        autoResetEvent.Set();
    }
}


void Worker()
{
    while (true)
    {
        Console.WriteLine($"{Thread.CurrentThread.Name} is waiting for signal.");
        // Wait for the signal from the main thread
        autoResetEvent.WaitOne();

        Console.WriteLine($"{Thread.CurrentThread.Name} proceeds.");
        // Simulate processing time
        Thread.Sleep(2000);
    }
}



///////////////////////////////////////////////
// Multiple Worker threads
//////////////////////////////////////////////
using AutoResetEvent autoResetEvent = new AutoResetEvent(false);
string? userInput = null;


// Start the worker thread
Thread workerThread = new Thread(Worker);
workerThread.Start();

// Main thread receives user input
Console.WriteLine("Server is running. Type 'exit' to stop.");
while (true)
{
    userInput = Console.ReadLine();

    // Signal the worker thread if input is "go"
    if (userInput.ToLower() == "go")
    {
        autoResetEvent.Set();
    }
}


void Worker()
{
    while (true)
    {
        Console.WriteLine("Worker thread is waiting for signal.");
        // Wait for the signal from the main thread
        autoResetEvent.WaitOne();

        Console.WriteLine("Worker thread proceeds.");
        // Simulate processing time
        Thread.Sleep(2000);
    }
}

