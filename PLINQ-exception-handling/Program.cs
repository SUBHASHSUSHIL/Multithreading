var items = Enumerable.Range(1, 20);

ParallelQuery<int> evenNumbers = null;


evenNumbers = items.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered).Where(x =>
{
    Console.WriteLine($"Processing number {x}; Thread Id: {Thread.CurrentThread.ManagedThreadId}");

    if (x == 5) throw new InvalidOperationException("This is intentional 5");

    if (x == 20) throw new ArgumentNullException("This is intentional 20");

    return (x % 2 == 0);
});


Console.WriteLine();

try
{
    evenNumbers.ForAll(item =>
    {
        Console.WriteLine($"{item}: Thread Id: {Thread.CurrentThread.ManagedThreadId}");
    });
}
catch (AggregateException ex)
{
    ex.Handle(x =>
    {
        Console.WriteLine(x.Message);
        return true;
    });
}
