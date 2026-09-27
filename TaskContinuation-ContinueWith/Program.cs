using System.Text.Json;

using var client = new HttpClient();
var task = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");
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