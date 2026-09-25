int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

int sum = 0;

var startTime = DateTime.Now;

foreach (var arr in array)
{
    Thread.Sleep(100);
    sum += arr;
}

var endTime = DateTime.Now;

var timespan = endTime - startTime;

Console.WriteLine($"Sum of array elements: {sum}");
Console.WriteLine($"Time taken: {timespan.TotalMilliseconds} ms");

Console.ReadLine();