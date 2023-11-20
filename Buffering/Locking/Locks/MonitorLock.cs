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

    public event EventHandler? Locking;
    public event EventHandler? AfterLocked;
    public event EventHandler? Unlocking;
    public event EventHandler? AferUnlocked;

    public ResourceLockHandle Lock(ResourceAccessFlag flags = ResourceAccessFlag.Generic)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        Monitor.Enter(this);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlag.Generic);
    }

    public bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, flags);
        Locking?.Invoke(this, EventArgs.Empty);
        var attempt = Monitor.TryEnter(this);
        if (attempt)
            AfterLocked?.Invoke(this, EventArgs.Empty);
        return attempt;
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        Unlocking?.Invoke(this, EventArgs.Empty);
        Monitor.Exit(this);
        AferUnlocked?.Invoke(this, EventArgs.Empty);
    }

    public IResourceLock Copy()
    {
        return new MonitorLock();
    }
}