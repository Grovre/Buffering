using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Like monitor but keeps the CPU spinning for the next possible entry
/// </summary>
public class SpinnerLock : IResourceLock
{
    private SpinLock _lock = new(false);
    
    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        var lockTaken = false;
        _lock.TryEnter(10_000, ref lockTaken);
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        var lockTaken = false;
        hlock = new(this, flags);
        _lock.TryEnter(TimeSpan.Zero, ref lockTaken);
        return lockTaken;
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);

        _lock.Exit();
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new SpinnerLock();
    }
}