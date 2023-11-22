using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a monitor to lock the front buffer for reading/writing.
/// Best use case is for two threads where one is reading the buffer and the other is updating the buffer.
/// ResourceAccessFlags is ignored as only one thread can enter the monitor at a time.
/// </summary>
public class MonitorLock : IResourceLock
{
    // TODO: Use dedicated private lock object

    /// <inheritdoc />
    public event EventHandler? Locking;
    /// <inheritdoc />
    public event EventHandler? AfterLocked;
    /// <inheritdoc />
    public event EventHandler? Unlocking;
    /// <inheritdoc />
    public event EventHandler? AfterUnlocked;

    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        Monitor.Enter(this);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, flags);
        Locking?.Invoke(this, EventArgs.Empty);
        var attempt = Monitor.TryEnter(this);
        if (attempt)
            AfterLocked?.Invoke(this, EventArgs.Empty);
        return attempt;
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        Unlocking?.Invoke(this, EventArgs.Empty);
        Monitor.Exit(this);
        AfterUnlocked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new MonitorLock();
    }
}