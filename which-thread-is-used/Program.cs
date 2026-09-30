class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"1. Main thread id:{Thread.CurrentThread.ManagedThreadId}");

        Console.WriteLine("Starting to do work.");
        var data = await FetchDataAsync();
        Console.WriteLine($"Data is fetched: {data}");

        Console.WriteLine($"2. Thread id:{Thread.CurrentThread.ManagedThreadId}");

        Console.WriteLine("Press enter to exit.");
        Console.ReadLine();
    }

    static async Task<string> FetchDataAsync()
    {
        Console.WriteLine($"3. Thread id:{Thread.CurrentThread.ManagedThreadId}");

        await Task.Delay(2000);

        Console.WriteLine($"4. Thread id:{Thread.CurrentThread.ManagedThreadId}");

        return "Complex data";
    }

}