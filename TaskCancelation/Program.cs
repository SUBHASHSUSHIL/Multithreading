/////////////////////////////////////////////////////////////////////////
// Example 1:

using var cts = new CancellationTokenSource();
var token = cts.Token;

var task = Task.Run(Work, token);

Console.WriteLine("To cancel, press 'c'");
var input = Console.ReadLine();
if (input == "c")
{
    cts.Cancel();
}


task.Wait();
Console.WriteLine($"Task status is: {task.Status}");
Console.ReadLine();

void Work()
{
    Console.WriteLine("Started doing the work.");

    for (int i = 0; i < 100000; i++)
    {
        Console.WriteLine($"{DateTime.Now}");

        if (token.IsCancellationRequested)
        {
            Console.WriteLine($"User requested cancellation at iteration: {i}");
            //break;
            //throw new OperationCanceledException();
            token.ThrowIfCancellationRequested();
        }

        Thread.SpinWait(30000000);
    }

    Console.WriteLine("Work is done.");

}

////////////////////////////////////////////////////////////////////////////
// Example 2:
using CancellationTokenSource source = new CancellationTokenSource();
source.CancelAfter(10);

using var client = new HttpClient();
var taskPokemonListJson = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon", source.Token);

Console.WriteLine(taskPokemonListJson.Result);