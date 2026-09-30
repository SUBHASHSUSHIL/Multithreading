
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

    var options = new ParallelOptions { CancellationToken = cts.Token };

    try
    {
        Parallel.For(0, 100000, options, i =>
        {
            Console.WriteLine($"{DateTime.Now}");
            Thread.SpinWait(30000000);
        });
    }
    catch (AggregateException ex)
    {
        Console.WriteLine(ex.ToString());
    }

    Console.WriteLine("Work is done.");
}
