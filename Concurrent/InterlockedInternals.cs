using System.Collections.Concurrent;

namespace Concurrent;

public class InterlockedInternals
{
    public static async Task ExecuteAsync()
    {
        int counter = 0;
        var tasks = new List<Task>();
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() => Interlocked.Add(ref counter, 5)));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine(counter);

    }
}