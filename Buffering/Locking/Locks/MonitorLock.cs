using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a monitor to lock the front buffer for reading/writing.
/// Best use case is for two threads where one is reading the buffer and the other is updating the buffer.
/// ResourceAccessFlag is ignored as only one thread can enter the monitor at a time.
/// </summary>
public class MonitorLock : IResourceLock
{
    // TODO: Use dedicated private lock object
    
    public ResourceLockHandle Lock(ResourceAccessFlag flags = ResourceAccessFlag.Generic)
    {
        Monitor.Enter(this);
        return new ResourceLockHandle(this, ResourceAccessFlag.Generic);
    }

    public bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, flags);
        return Monitor.TryEnter(this);
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        Monitor.Exit(this);
    }

    public IResourceLock Copy()
    {
        return new MonitorLock();
    }
}