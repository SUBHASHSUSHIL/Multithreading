var items = Enumerable.Range(1, 200);

var evenNumbers = items.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered).Where(x =>
{
    Console.WriteLine($"Processing number {x}; Thread Id: {Thread.CurrentThread.ManagedThreadId}");
    return (x % 2 == 0);
});

Console.WriteLine();
//Console.WriteLine($"There are {evenNumbers.Count()} even numbers in the collection.");

foreach (var item in evenNumbers)
{
    Console.WriteLine($"{item}: Thread Id: {Thread.CurrentThread.ManagedThreadId}");
}