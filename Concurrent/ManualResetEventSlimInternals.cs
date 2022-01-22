namespace Concurrent;

public class ManualResetEventSlimInternals
{
    private static ManualResetEventSlim AutoEvent = new ManualResetEventSlim(false);
    private static int TasksCount = 10;
    private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public static async Task ExecuteAsync()
    {
        var token = _cancellationTokenSource.Token;
        var tasks = new List<Task>();
        for (int i = 0; i <= TasksCount; i++)
        {
            var i1 = i;
            Task task = Task.Run(() => ThreadProc("My data: " + i1, token), token);
            tasks.Add(task);
        }

        Console.WriteLine("Press enter to run all threads");
        Console.Read();
        _cancellationTokenSource.Cancel();

        await Task.WhenAll(tasks);
    }

    private static async Task ThreadProc(string someData, CancellationToken token)  
    {  
        int id = Thread.CurrentThread.ManagedThreadId;
        try
        {
            AutoEvent.Wait(token);
            await Task.Delay(500, token);
            Console.WriteLine(id + " job done. " + someData);
            AutoEvent.Set();
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine(id + " job cancelled. ");
        }
    }
}