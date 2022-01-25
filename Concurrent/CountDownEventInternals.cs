namespace Concurrent;

public static class CountDownEventInternals
{
    public static async Task ExecuteAsync(CancellationToken token)
    {
        await Task.CompletedTask;
        
        int numOfTasks = 10;

        using CountdownEvent countdownEvent = new CountdownEvent(numOfTasks);
        
        int[] result = new int[numOfTasks];
        var tasks = new List<Task>();
        for (int i = 0; i < numOfTasks; ++i)
        {
            int j = i;
            tasks.Add(Task.Factory.StartNew(() =>
            {
                throw new InvalidOperationException();
                countdownEvent.Signal();
                result[j] = j * 10;
            }));
        }

        await Task.WhenAll(tasks);
        countdownEvent.Wait(token);
 
        foreach (var r in result)
        {
            Console.WriteLine(r);
        }
             
        Console.ReadLine();
    }
}