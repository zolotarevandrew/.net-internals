namespace Concurrent;

public class ThreadPoolInternals
{
    public static async Task ExecuteAsync()
    {
        Console.WriteLine(SynchronizationContext.Current == null ? "null" : "not null");
        
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var i1 = i + 1;
            tasks.Add(Task.Run(() => Console.WriteLine(i1)));
        }
        
        await Task.WhenAll(tasks);
        ShowThreads();

        tasks.Clear();
        for (int i = 0; i < 50; i++)
        {
            var i1 = i + 1;
            tasks.Add(Task.Run(TryEnterToLock));
        }
        
        await Task.WhenAll(tasks);
        ShowThreads();
    }

    static async Task TryEnterToLock()
    {
        var semaphore = new SemaphoreSlim(1, 1);
        bool lockTaken = false;
        try
        {
            lockTaken = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            if (lockTaken) semaphore.Release();
        }
    }

    static void ShowThreads()
    {
        Console.WriteLine("\r\n");
        Console.WriteLine("\r\n");
        Console.WriteLine(ThreadPool.ThreadCount);
    }
}