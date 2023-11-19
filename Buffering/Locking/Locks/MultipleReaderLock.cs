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

    public ResourceLockHandle Lock(ResourceAccessFlag flags = ResourceAccessFlag.Generic)
    {
        if ((flags & ResourceAccessFlag.Write) != 0)
        {
            return WriteLock();
        }

        if ((flags & ResourceAccessFlag.Read) != 0)
        {
            return ReadLock();
        }
        
        throw new NotSupportedException("Generic or unspecified buffer access not supported");
    }

    public bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock)
    {
        // TODO: support
        throw new NotImplementedException(
            "Lazy, I'll come back to this");
    }

    public ResourceLockHandle ReadLock()
    {
        _lock.EnterReadLock();
        return new ResourceLockHandle(this, ResourceAccessFlag.Read);
    }

    public ResourceLockHandle WriteLock()
    {
        _lock.EnterWriteLock();
        return new ResourceLockHandle(this, ResourceAccessFlag.Write);
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);
        
        if ((hlock.AccessFlags & ResourceAccessFlag.Write) != 0)
            _lock.ExitWriteLock();
        else if ((hlock.AccessFlags & ResourceAccessFlag.Read) != 0)
            _lock.ExitReadLock();
        else
            throw new NotSupportedException("Lock must be a write or read lock");
    }

    public IResourceLock Copy()
    {
        return new MultipleReaderLock();
    }
}