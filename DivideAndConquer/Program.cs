int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

int sum = 0;

foreach (var arr in array)
{
    sum += arr;
}

Console.WriteLine($"Sum of array elements: {sum}");