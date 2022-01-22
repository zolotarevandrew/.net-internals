namespace Concurrent;

public class ManualResetEventInternals
{
    private static ManualResetEvent AutoEvent = new ManualResetEvent(false);
    private static int TasksCount = 10;

    public static async Task ExecuteAsync()
    {
        var tasks = new List<Task>();
        for (int i = 0; i <= TasksCount; i++)
        {
            var i1 = i;
            Task task = Task.Run(() => ThreadProc("My data: " + i1));
            tasks.Add(task);
        }

        Console.WriteLine("Press enter to run all threads");
        Console.Read();
        AutoEvent.Set();

        await Task.WhenAll(tasks);
    }

    private static async Task ThreadProc(string someData)  
    {  
        
        int id = Thread.CurrentThread.ManagedThreadId;  
        
        AutoEvent.WaitOne();  

        await Task.Delay(500);
        Console.WriteLine(id + " job done. " + someData);
        AutoEvent.Set();
    }
}