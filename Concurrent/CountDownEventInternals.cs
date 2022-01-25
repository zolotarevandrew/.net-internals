namespace Concurrent;

public static class CountDownEventInternals
{
    public static async Task ExecuteAsync(CancellationToken token)
    {
        await Task.CompletedTask;
        
        int numOfTasks = 10;
        
        CountdownEvent countdownEvent = new CountdownEvent(numOfTasks);

        int[] result = new int[numOfTasks];
        for (int i = 0; i < numOfTasks; ++i)
        {
            int j = i;
            Task.Factory.StartNew( async () =>
            {
                countdownEvent.Signal();
                await Task.Delay(500, token);
                result[j] = j * 10;
            });
        }
 
        countdownEvent.Wait(token);
 
        foreach (var r in result)
        {
            Console.WriteLine(r);
        }
             
        Console.ReadLine();
    }
}