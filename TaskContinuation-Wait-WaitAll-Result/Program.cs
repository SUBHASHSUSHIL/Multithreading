///////////////////////////////////////////////
// Using Wait
///////////////////////////////////////////////
int sum = 0;

var task = Task.Run(() =>
{
    for (int i = 1; i <= 100; i++)
    {
        Task.Delay(100);
        sum += i;
    }
});

task.Wait();

Console.WriteLine($"The result is: {sum}");
Console.ReadLine();


///////////////////////////////////////////////
// Using Result 1
///////////////////////////////////////////////
var task = Task.Run(() =>
{
    int sum = 0;

    for (int i = 1; i <= 100; i++)
    {
        Task.Delay(100);
        sum += i;
    }

    return sum;
});

Console.WriteLine($"The result is: {task.Result}");
Console.ReadLine();


///////////////////////////////////////////////
// Using Result 2
///////////////////////////////////////////////

using var client = new HttpClient();
var task = client.GetStringAsync("https://pokeapi.co/api/v2/pokemon");
var result = task.Result;

Console.WriteLine(result);