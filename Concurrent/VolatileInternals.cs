namespace Concurrent;

public class VolatileInternals
{
    public static async Task ExecuteAsync()
    {
        bool Stop = false;
        var stop = Stop;
        var t1 = Task.Run(() => Worker(ref stop));
        var t2 = Task.Run(() => Worker(ref stop));
        var t3 = Task.Run(() => Worker(ref stop));
        await Task.Delay(2000);
        Volatile.Write(ref stop, true);
        await Task.WhenAll(t1, t2, t3);
    }

    static void Worker(ref bool stop)
    {
        Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
        while (Volatile.Read(ref stop) != true)
        {
            Console.WriteLine("working");
        }
    }

}