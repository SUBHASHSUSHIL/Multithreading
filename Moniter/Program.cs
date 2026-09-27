// Moniter to add timeout for lock

// counter example

int counter = 0;

object counterLock = new object();

Thread thread1 = new Thread(IncrementCounter);
Thread thread2 = new Thread(IncrementCounter);
thread1.Start();
thread2.Start();


thread1.Join();
thread2.Join();

Console.WriteLine($"Final counter value is: {counter}");


void IncrementCounter()
{
    for (int i = 0; i < 100000; i++)
    {
        Monitor.Enter(counterLock);     // Acquire the lock before modifying the counter
        try
        {
            counter = counter + 1;
        }
        finally
        {
            Monitor.Exit(counterLock);
        }
    }
}

/////////////////////////////////////////
/// tickets booking system //////////////
/////////////////////////////////////////
Queue<string?> requestQueue = new Queue<string?>();

int availableTickets = 10;
object ticketsLock = new object();

// 2. Start the requests queue monitoring thread
Thread monitoringThread = new Thread(MonitorQueue);
monitoringThread.Start();

// 1. Enqueue the requests
Console.WriteLine("Server is running. \r\n Type 'b' to book a ticket. \r\n Type 'c' to cancel. \r\n Type 'exit' to stop. \r\n");
while (true)
{
    string? input = Console.ReadLine();
    if (input?.ToLower() == "exit")
    {
        break;
    }

    requestQueue.Enqueue(input);
}

void MonitorQueue()
{
    while (true)
    {
        if (requestQueue.Count > 0)
        {
            string? input = requestQueue.Dequeue();
            Thread processingThread = new Thread(() => ProcessBooking(input));
            processingThread.Start();
        }
        Thread.Sleep(100);
    }
}

// 3. Processing the requests
void ProcessBooking(string? input)
{

    if (Monitor.TryEnter(ticketsLock, 2000))
    {
        try
        {
            // Simulate processing time
            Thread.Sleep(3000);

            if (input == "b")
            {
                if (availableTickets > 0)
                {
                    availableTickets--;
                    Console.WriteLine();
                    Console.WriteLine($"Your seat is booked. {availableTickets} seats are still available.");
                }
                else
                {
                    Console.WriteLine($"Tickets are not available.");
                }
            }
            else if (input == "c")
            {
                if (availableTickets < 10)
                {
                    availableTickets++;
                    Console.WriteLine();
                    Console.WriteLine($"Your booking is canceled. {availableTickets} seats are available.");
                }
                else
                {
                    Console.WriteLine($"Error. You cannot cancel a booking at this time.");
                }
            }
        }
        finally
        {
            Monitor.Exit(ticketsLock);
        }
    }
    else
    {
        Console.WriteLine("The system is busy. Please wait.");
    }

}
