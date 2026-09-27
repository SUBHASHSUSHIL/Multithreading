bool cancelThread = false;

Thread thread = new Thread(Work);
thread.Start();

Console.WriteLine("To cancel, press 'c'");
var input = Console.ReadLine();
if (input == "c")
{
    cancelThread = true;
}

thread.Join();
Console.ReadLine();

void Work()
{
    Console.WriteLine("Started doing the work.");

    for (int i = 0; i < 100000; i++)
    {
        if (cancelThread)
        {
            Console.WriteLine($"User requested cancellation at iteration: {i}");
            break;
        }

        Thread.SpinWait(300000);
    }

    Console.WriteLine("Work is done.");

}