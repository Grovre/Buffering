using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Like monitor but keeps the CPU spinning for the next possible entry
/// </summary>
public class SpinnerLock : IResourceLock
{
    /// <inheritdoc />
    public event EventHandler? Locking;
    /// <inheritdoc />
    public event EventHandler? AfterLocked;
    /// <inheritdoc />
    public event EventHandler? Unlocking;
    /// <inheritdoc />
    public event EventHandler? AfterUnlocked;

    private SpinLock _lock = new(false);
    
    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        var lockTaken = false;
        _lock.TryEnter(10_000, ref lockTaken);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        var lockTaken = false;
        hlock = new(this, flags);
        _lock.TryEnter(TimeSpan.Zero, ref lockTaken);
        if (lockTaken)
            AfterLocked?.Invoke(this, EventArgs.Empty);
        return lockTaken;
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);

        Unlocking?.Invoke(this, EventArgs.Empty);
        _lock.Exit();
        AfterUnlocked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new SpinnerLock();
    }
}