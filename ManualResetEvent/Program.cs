
using ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(false);

Console.WriteLine("Press enter to release all threads...");

for (int i = 1; i <= 3; i++)
{
    Thread thread = new Thread(Work);
    thread.Name = $"Thread {i}";
    thread.Start();
}


Console.ReadLine();

manualResetEvent.Set();

Console.ReadLine();

void Work()
{
    Console.WriteLine($"{Thread.CurrentThread.Name} is waiting for the signal...");
    manualResetEvent.Wait();
    Thread.Sleep(1000);
    Console.WriteLine($"{Thread.CurrentThread.Name} has been released.");
}