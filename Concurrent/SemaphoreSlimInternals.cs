namespace Concurrent;

public static class SemaphoreSlimInternals
{
    private static SemaphoreSlim mutex = new SemaphoreSlim(numThreads, numThreads);  
    private const int numhits = 1;  
    private const int numThreads = 4;  
    
    private static async Task TaskProcess(int id)  
    {  
        for (int i = 0; i < numhits; i++)
        {
            bool lockTaken = false;

            try
            {
                lockTaken = await mutex.WaitAsync(TimeSpan.Zero);
                if (lockTaken)
                {
                    Console.WriteLine("{0} has entered", id);
                    await Task.Delay(500).ConfigureAwait(false);
                }
            }
            finally
            {
                if (lockTaken) Console.WriteLine("{0} is leaving, available {1}", id, mutex.Release(1));    
                else Console.WriteLine("{0} lock not taken", id);
            }
        }
    }

    public static async Task ExecuteSimpleAsync()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < numThreads * 2; i++)
        {
            int id = i;
            var task = Task.Run(() => TaskProcess(id));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        //will never be executed
        Console.WriteLine("Done");  
    }  
}