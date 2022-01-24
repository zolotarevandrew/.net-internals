namespace Concurrent;

public class ReadeWriterLockSlimInternals
{
    public static void ExecuteAsync()
    {
        using var wrapper = new ReadWriteLockWrapper();
        var fields = new List<string>();
        using (wrapper.EnterReadLock())
        {
            for (int i = 0; i < 100; i++)
            {
                var thread = new Thread(GetFieldsAsync);
                thread.Start();
            }

            void GetFieldsAsync()
            {
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + "Entered to get fields");
                if (fields.Count > 0)
                {
                    Console.Write(fields[0]);
                }
            }
        }
        using (wrapper.EnterWriteLock())
        {
            for (int i = 0; i < 5; i++)
            {
                var i1 = i;
                var thread = new Thread(() => SetFieldsAsync(new()
                {
                    i1.ToString()
                }));
                thread.Start();
            }

            void SetFieldsAsync(List<string> str)
            {
                fields = str;
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + "Entered to set fields");
                Thread.Sleep(500);
            }
        }
        Thread.Sleep(1000);
        Console.WriteLine(fields[0]);
    }
}

public class ReadWriteLockWrapper : IDisposable
{
    public readonly struct WriteLockToken : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        public WriteLockToken(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            @lock.EnterWriteLock();
        }
        public void Dispose() => _lock.ExitWriteLock();
    }

    public readonly struct ReadLockToken : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        public ReadLockToken(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            @lock.EnterReadLock();
        }
        public void Dispose() => _lock.ExitReadLock();
    }

    private readonly ReaderWriterLockSlim _lock = new();
    
    public ReadLockToken EnterReadLock() => new ReadLockToken(_lock);
    public WriteLockToken EnterWriteLock() => new(_lock);

    public void Dispose() => _lock.Dispose();
}