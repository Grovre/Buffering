using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a ReaderWriterLockSlim in order to synchronize access to a buffer.
/// Best use case is when multiple reader threads are involved.
/// Access flags must not be generic. They must be read or write.
/// </summary>
public class MultipleReaderLock : IResourceLock
{
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
        _lock.EnterReadLock();
        return new ResourceLockHandle(this, ResourceAccessFlags.Read);
    }

    internal ResourceLockHandle WriteLock()
    {
        _lock.EnterWriteLock();
        return new ResourceLockHandle(this, ResourceAccessFlags.Write);
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        if ((hlock.AccessFlags & ResourceAccessFlags.Write) != 0)
            _lock.ExitWriteLock();
        else if ((hlock.AccessFlags & ResourceAccessFlags.Read) != 0)
            _lock.ExitReadLock();
        else
            throw new NotSupportedException("Lock must be a write or read lock");
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new MultipleReaderLock();
    }
}