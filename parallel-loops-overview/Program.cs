int[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

int sum = 0;
object lockSum = new object();

Parallel.For(0, array.Length, i =>
{
    lock (lockSum)
    {
        sum += array[i];
    }
});

//Parallel.ForEach(array, i =>
//{
//    lock (lockSum)
//    {
//        sum += i;   
//    }
//});

Console.WriteLine($"The sum is {sum}");

Console.ReadLine();


Parallel.Invoke(() =>
{
    Console.WriteLine("I am one.");
},
() =>
{
    Console.WriteLine("I am two.");
},
() =>
{
    Console.WriteLine("I am three.");
});

Console.ReadLine();

