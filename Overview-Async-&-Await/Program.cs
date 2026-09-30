using System.Text.Json;

OutputFirstPokemon();

Console.WriteLine("This is the end of the program.");
Console.ReadLine();

async void OutputFirstPokemon()
{
    using var client = new HttpClient();
    var taskGetPokemonList = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

    var response = await taskGetPokemonList;

    var doc = JsonDocument.Parse(response);
    JsonElement root = doc.RootElement;
    JsonElement results = root.GetProperty("results");
    JsonElement firstPokemon = results[0];

    Console.WriteLine($"First pokemon name: {firstPokemon.GetProperty("name")}");
    Console.WriteLine($"First pokemon url: {firstPokemon.GetProperty("url")}");
}