using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a monitor to lock the front buffer for reading/writing.
/// Best use case is for two threads where one is reading the buffer and the other is updating the buffer.
/// ResourceAccessFlags is ignored as only one thread can enter the monitor at a time.
/// </summary>
public class MonitorLock : IResourceLock
{
    private readonly object _lock = new();

    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        Monitor.Enter(_lock);
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, flags);
        var attempt = Monitor.TryEnter(_lock);
        return attempt;
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        Monitor.Exit(_lock);
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new MonitorLock();
    }
}