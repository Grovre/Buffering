using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a ReaderWriterLockSlim in order to synchronize access to a buffer.
/// Best use case is when multiple reader threads are involved.
/// Access flags must not be generic. They must be read or write.
/// </summary>
public class MultipleReaderLock : IResourceLock
{
    /// <inheritdoc />
    public event EventHandler? Locking;
    /// <inheritdoc />
    public event EventHandler? AfterLocked;
    /// <inheritdoc />
    public event EventHandler? Unlocking;
    /// <inheritdoc />
    public event EventHandler? AfterUnlocked;
    
    private readonly ReaderWriterLockSlim _lock = new();

    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        if ((flags & ResourceAccessFlags.Write) != 0)
        {
            return WriteLock();
        }

        if ((flags & ResourceAccessFlags.Read) != 0)
        {
            return ReadLock();
        }
        
        throw new NotSupportedException("Generic or unspecified buffer access not supported");
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        // TODO: support
        throw new NotImplementedException(
            "Trying to lock is not supported.");
    }

    internal ResourceLockHandle ReadLock()
    {
        Locking?.Invoke(this, EventArgs.Empty);
        _lock.EnterReadLock();
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlags.Read);
    }

    internal ResourceLockHandle WriteLock()
    {
        Locking?.Invoke(this, EventArgs.Empty);
        _lock.EnterWriteLock();
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlags.Write);
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        Unlocking?.Invoke(this, EventArgs.Empty);
        if ((hlock.AccessFlags & ResourceAccessFlags.Write) != 0)
            _lock.ExitWriteLock();
        else if ((hlock.AccessFlags & ResourceAccessFlags.Read) != 0)
            _lock.ExitReadLock();
        else
            throw new NotSupportedException("Lock must be a write or read lock");
        AfterUnlocked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new MultipleReaderLock();
    }
}