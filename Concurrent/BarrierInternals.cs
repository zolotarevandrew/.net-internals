namespace Concurrent;

public class BarrierInternals
{
    public static async Task ExecuteAsync()
    {
        await Task.CompletedTask;
        int tasksCount = 5;
        using var barrier = new Barrier(tasksCount, (b) =>
        {
            Console.WriteLine("post phase");
        });
        barrier.RemoveParticipant();
        
        var tasks = new List<Task>();
        tasks.Add(Method1());
        tasks.Add(Method2());
        tasks.Add(Method3());
        tasks.Add(Method4());
        tasks.Add(Method5());
        
        async Task Method1()
        {
            await Task.Delay(500);
            Console.WriteLine("method1");
            barrier.SignalAndWait();
        }
        
        async Task Method2()
        {
            await Task.Delay(300);
            Console.WriteLine("method2");
            barrier.SignalAndWait();
        }
        
        async Task Method3()
        {
            await Task.Delay(1000);
            Console.WriteLine("method3");
            barrier.SignalAndWait();
        }
        
        async Task Method4()
        {
            await Task.Delay(2000);
            Console.WriteLine("method4");
            barrier.SignalAndWait();
        }

        async Task Method5()
        {
            await Task.Delay(5000);
            Console.WriteLine("method5");
            barrier.SignalAndWait();
        }
        
        await Task.WhenAll(tasks);
    }
}