int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

int SumSegment(int start, int end)
{
    int segmentSum = 0;
    for (int i = start; i < end; i++)
    {
        Thread.Sleep(100);
        segmentSum += array[i];
    }

    return segmentSum;
}

int numofThreads = 4;
int segmentLength = array.Length / numofThreads;

Task<int>[] tasks = new Task<int>[numofThreads];
tasks[0] = Task.Run(() => { return SumSegment(0, segmentLength); });
tasks[1] = Task.Run(() => { return SumSegment(segmentLength, 2 * segmentLength); });
tasks[2] = Task.Run(() => { return SumSegment(2 * segmentLength, 3 * segmentLength); });
tasks[3] = Task.Run(() => { return SumSegment(3 * segmentLength, array.Length); });

Task.WhenAll(tasks).ContinueWith(t =>  // whenall returns a task that will complete when all of the provided tasks have completed. The result of the returned task is an array containing the results of each of the individual tasks.
{
    Console.WriteLine($"The summary is {t.Result.Sum()}");
});

Console.WriteLine("This is the end of the program.");

Console.ReadLine();