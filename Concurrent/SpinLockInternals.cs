using System.Collections.Concurrent;

namespace Concurrent;

public class SpinLockInternals
{
    public static async Task ExecuteAsync()
    {
        string str = "5";
        var spin = new SpinLock();

        var tasks = new List<Task>();
        for (int i = 0; i < 15; i++)
        {
            var i1 = i;
            Thread th = new Thread(() =>Execute(i1.ToString()));
            th.Start();
        }

        void Execute(string newStr)
        {
            bool lockTaken = false;
            try
            {
                spin.Enter(ref lockTaken);
                str = newStr;
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + "entered the lock with " + newStr);
            }
            finally
            {
                if (lockTaken) spin.Exit(true);
            }
        }

        //await Task.WhenAll(tasks);
        Console.WriteLine(str);
    }
}