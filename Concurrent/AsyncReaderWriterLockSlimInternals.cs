using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Concurrent;

public static class AsyncReaderWriterLockSlimInternals
{
    public static async Task ExecuteAsync()
    {
        using var wrapper = new AsyncReadWriteLockWrapper();
        var fields = new List<string>();
        var tasks = new List<Task>();
        await using (wrapper.EnterReadLock())
        {
            for (int i = 0; i < 100; i++)
            {
                var task = Task.Run(GetFieldsAsync);
                tasks.Add(task);
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
        await using (wrapper.EnterWriteLock())
        {
            for (int i = 0; i < 5; i++)
            {
                var i1 = i;
                var task = Task.Run(() => SetFieldsAsync(new()
                {
                    i1.ToString()
                }));
                tasks.Add(task);
            }

            async Task SetFieldsAsync(List<string> str)
            {
                fields = str;
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + "Entered to set fields");
            }
        }
        
        await Task.WhenAll(tasks);
        Console.WriteLine("done");
    }
}

public class AsyncReadWriteLockWrapper : IDisposable
{
    public readonly struct WriteLockToken : IAsyncDisposable
    {
        private readonly AsyncReaderWriterLock _lock;
        public WriteLockToken(AsyncReaderWriterLock @lock)
        {
            _lock = @lock;
        }
        public async ValueTask DisposeAsync() => await _lock.WriteLockAsync();
    }

    public readonly struct ReadLockToken : IAsyncDisposable
    {
        private readonly AsyncReaderWriterLock _lock;
        public ReadLockToken(AsyncReaderWriterLock @lock)
        {
            _lock = @lock;
        }
        public async ValueTask DisposeAsync() => await _lock.ReadLockAsync();
    }

    private readonly AsyncReaderWriterLock _lock = new();
    
    public ReadLockToken EnterReadLock() => new(_lock);
    public WriteLockToken EnterWriteLock() => new(_lock);

    public void Dispose() => _lock.Dispose();
}