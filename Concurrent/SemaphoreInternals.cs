namespace Concurrent;

public static class SemaphoreInternals
{
    private static Semaphore mutex = new Semaphore(numThreads, numThreads);  
    private const int numhits = 1;  
    private const int numThreads = 4;  
    
    private static async Task TaskProcess()  
    {  
        for (int i = 0; i < numhits; i++)  
        {  
            mutex.WaitOne();  
            Console.WriteLine("{0} has entered", Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(500).ConfigureAwait(false);
            Console.WriteLine("{0} is leaving, available {1}", Thread.CurrentThread.ManagedThreadId, mutex.Release(1));
        }
    }

    public static async Task ExecuteSimpleAsync()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < numThreads * 2; i++)
        {
            var task = Task.Run(TaskProcess);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        //will never be executed
        Console.WriteLine("Done");  
    }  
}