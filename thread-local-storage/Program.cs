int[] array = Enumerable.Range(1, 10).ToArray();

int sum = 0;
object lockSum = new object();

Parallel.For(
    0,
    array.Length,
    () => 0,
    (i, state, tls) =>
    {
        tls += array[i];
        return tls;
    },
    tls =>
    {
        lock (lockSum)
        {
            sum += tls;
            Console.WriteLine($"The task id: {Task.CurrentId}");
        }
    }
);

Console.WriteLine($"The sum is {sum}");

Console.ReadLine();