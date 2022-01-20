namespace Concurrent;

public static class MutexInternals
{
    private static Mutex mutex = new Mutex();  
    private const int numhits = 1;  
    private const int numThreads = 4;  
    
    private static async Task TaskProcess()  
    {  
        for (int i = 0; i < numhits; i++)  
        {  
            mutex.WaitOne();  
            Console.WriteLine("{0} has entered in the C_sharpcorner.com", Thread.CurrentThread.ManagedThreadId);  
          
            await Task.Delay(500).ConfigureAwait(false);  
            
            Console.WriteLine("{0} is leaving the C_sharpcorner.com\r\n", Thread.CurrentThread.ManagedThreadId);  
            mutex.ReleaseMutex();   
        }
    }

    public static async Task ExecuteSimpleAsync()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < numThreads; i++)
        {
            var task = Task.Run(TaskProcess);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        //will never be executed
        Console.WriteLine("Done");  
    }  
}