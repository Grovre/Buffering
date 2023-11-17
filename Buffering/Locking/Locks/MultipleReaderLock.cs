using System.Diagnostics;

namespace Buffering.Locking.Locks;

public class MultipleReaderLock : IBufferLock
{
    private readonly ReaderWriterLockSlim _lock = new();

    public LockHandle Lock(BufferAccessFlag flags = BufferAccessFlag.Generic)
    {
        if ((flags & BufferAccessFlag.Write) != 0)
        {
            return WriteLock();
        }

        if ((flags & BufferAccessFlag.Read) != 0)
        {
            return ReadLock();
        }
        
        throw new Exception("Generic or unspecified buffer access not supported");
    }

    public LockHandle ReadLock()
    {
        _lock.EnterReadLock();
        return new LockHandle(this, BufferAccessFlag.Read);
    }

    public LockHandle WriteLock()
    {
        _lock.EnterWriteLock();
        return new LockHandle(this, BufferAccessFlag.Write);
    }

    public void Unlock(LockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new Exception("Lock handle not owned by lock queried for unlocking");
        
        if ((hlock.AccessFlags & BufferAccessFlag.Write) != 0)
            _lock.ExitWriteLock();
        else if ((hlock.AccessFlags & BufferAccessFlag.Read) != 0)
            _lock.ExitReadLock();
        else
            throw new Exception("Lock must be a write or read lock");
    }
}