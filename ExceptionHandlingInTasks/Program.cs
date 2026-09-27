// 1. Exceptions in Tasks are hidden. 

using System.Text.Json;

using var client = new HttpClient();
var task = client.GetStringAsync("https://pokeapi123.co/api/v2/pokemon");
task.ContinueWith(t =>
{
    var result = t.Result;
    var doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    JsonElement results = root.GetProperty("results");
    JsonElement firstPokemon = results[0];

    Console.WriteLine($"First pokemon name: {firstPokemon.GetProperty("name")}");
    Console.WriteLine($"First pokemon url: {firstPokemon.GetProperty("url")}");
});

Console.WriteLine("This is the end of the program.");
Console.ReadLine();

// 3. Exceptions are stored in the task itself. 
	using System.Text.Json;
	
	using var client = new HttpClient();
var task1 = client.GetStringAsync("https://pokeapi123.co/api/v2/pokemon");
var task2 = task1.ContinueWith(t =>
{
    var result = t.Result;
    var doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    JsonElement results = root.GetProperty("results");
    JsonElement firstPokemon = results[0];

    Console.WriteLine($"First pokemon name: {firstPokemon.GetProperty("name")}");
    Console.WriteLine($"First pokemon url: {firstPokemon.GetProperty("url")}");
});

Thread.Sleep(1000);
Console.WriteLine(task1.Status);
Console.WriteLine(task2.Status);

Console.WriteLine("This is the end of the program.");
Console.ReadLine();

Console.WriteLine("Press enter key to exit.");
Console.ReadLine();

// 4. Multiple ones can be stored hence we can iterate them

var tasks = new[]
{
        Task.Run(() => throw new InvalidOperationException("Invalid operation exception")),
        Task.Run(() => throw new ArgumentNullException("Argument null exception")),
        Task.Run(() => throw new Exception("General exception"))
    };

Task.WhenAll(tasks).ContinueWith(t =>
{
    if (t.IsFaulted && t.Exception != null)
    {
        foreach (var ex in t.Exception.InnerExceptions)
        {
            Console.WriteLine(ex.Message);
        }
    }

});

Console.WriteLine("Press enter key to exit.");
Console.ReadLine();

// 5. Using wait or result will make the stored exceptions thrown

var tasks = new[]
{
            Task.Run(() => throw new InvalidOperationException("Invalid operation exception")),
            Task.Run(() => throw new ArgumentNullException("Argument null exception")),
            Task.Run(() => throw new Exception("General exception"))
        };

var t = Task.WhenAll(tasks);

t.Wait();

Console.WriteLine("Press enter key to exit.");
Console.ReadLine();

