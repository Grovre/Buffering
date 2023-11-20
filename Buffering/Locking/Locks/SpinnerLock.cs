using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Like monitor but keeps the CPU spinning for the next possible entry
/// </summary>
public class SpinnerLock : IResourceLock
{
    public event EventHandler? Locking;
    public event EventHandler? AfterLocked;
    public event EventHandler? Unlocking;
    public event EventHandler? AferUnlocked;

    private SpinLock _lock = new(false);
    
    public ResourceLockHandle Lock(ResourceAccessFlag flags = ResourceAccessFlag.Generic)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        var lockTaken = false;
        _lock.TryEnter(10_000, ref lockTaken);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlag.Generic);
    }

    public bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        var lockTaken = false;
        hlock = new(this, flags);
        _lock.TryEnter(TimeSpan.Zero, ref lockTaken);
        if (lockTaken)
            AfterLocked?.Invoke(this, EventArgs.Empty);
        return lockTaken;
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);

        Unlocking?.Invoke(this, EventArgs.Empty);
        _lock.Exit();
        AferUnlocked?.Invoke(this, EventArgs.Empty);
    }

    public IResourceLock Copy()
    {
        return new SpinnerLock();
    }
}