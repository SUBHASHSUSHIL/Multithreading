Console.WriteLine("Hello, World!");

void WriteThreadId()
{
    for (int i = 0; i < 100; i++)
    {
        Console.WriteLine(Thread.CurrentThread.Name);
        Thread.Sleep(10);
    }
}

Thread thread1 = new Thread(WriteThreadId);
Thread thread2 = new Thread(WriteThreadId);

thread1.Name = "Thread 1";
thread2.Name = "Thread 2";
Thread.CurrentThread.Name = "Main Thread";

thread1.Priority = ThreadPriority.Highest;
thread2.Priority = ThreadPriority.Lowest;
Thread.CurrentThread.Priority = ThreadPriority.Normal;

thread1.Start();
thread2.Start();

WriteThreadId();

Console.ReadLine();
