namespace Concurrent;

public class TimedLock
{
    private readonly object _toLock;

    public TimedLock(object toLock)
    {
        this._toLock = toLock;
    }

    public LockReleaser Lock(TimeSpan timeout)
    {
        if (Monitor.TryEnter(_toLock, timeout))
        {
            return new LockReleaser(_toLock);
        }
        throw new TimeoutException();
    }

    public struct LockReleaser : IDisposable
    {
        private readonly object _toRelease;

        public LockReleaser(object toRelease)
        {
            this._toRelease = toRelease;
        }
        
        public void Dispose()
        {
            Monitor.Exit(_toRelease);
        }
    }

    public static void Process()
    {
        object guard = new object();

        using(new TimedLock(guard).Lock(TimeSpan.FromSeconds(2)))
        {
            
        }
    }
}

