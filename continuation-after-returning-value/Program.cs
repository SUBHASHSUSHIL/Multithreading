using System.Text.Json;
using var client = new HttpClient();
var pokemonListJson = await client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");

// Get the first Pokemon's url
var doc = JsonDocument.Parse(pokemonListJson);
JsonElement root = doc.RootElement;
JsonElement results = root.GetProperty("results");
JsonElement firstPokemon = results[0];
var url = firstPokemon.GetProperty("url").ToString();

// Get the first Pokemon's detailed info
var firstPokemonJson = await client.GetStringAsync(url);

// Get the weight and height
doc = JsonDocument.Parse(firstPokemonJson);
root = doc.RootElement;
Console.WriteLine($"Name: {root.GetProperty("name").ToString()}");
Console.WriteLine($"Weight: {root.GetProperty("weight").ToString()}");
Console.WriteLine($"Height: {root.GetProperty("height")}");
Console.WriteLine("This is the end of the program.");
Console.ReadLine();