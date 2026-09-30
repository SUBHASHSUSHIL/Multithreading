class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting to do work.");
        var data = await FetchDataAsync();
        Console.WriteLine($"Data is fetched: {data}");

        Console.WriteLine("Press enter to exit.");
        Console.ReadLine();
    }

    static async Task<string> FetchDataAsync()
    {
        await Task.Delay(2000);

        return "Complex data";
    }

}