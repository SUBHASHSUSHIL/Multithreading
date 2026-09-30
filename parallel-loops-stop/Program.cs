int[] array = Enumerable.Range(0, 100).ToArray();

int sum = 0;
object lockSum = new object();


try
{
    Parallel.For(0, array.Length, (i, state) =>
    {
        lock (lockSum)
        {
            if (!state.IsStopped)
            {
                if (i == 65)
                    state.Stop();

                sum += array[i];
                Console.WriteLine($"Current task id: {Task.CurrentId}; Is thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}");
            }
        }
    });


}
catch (AggregateException ex)
{
    Console.WriteLine(ex);
}

Console.WriteLine($"The sum is {sum}");

Console.ReadLine();

