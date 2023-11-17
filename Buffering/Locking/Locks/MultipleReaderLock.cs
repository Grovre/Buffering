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

    public void Unlock(LockHandle lhnd)
    {
        if (lhnd.Owner != this)
            throw new Exception("Lock handle not owned by lock queried for unlocking");

        if (lhnd.AccessFlag.HasFlag(BufferAccessFlag.Generic))
            throw new Exception("Generic flag not allowed in lock");
        
        if (lhnd.AccessFlag.HasFlag(BufferAccessFlag.Write))
            _lock.ExitWriteLock();
        else
            _lock.ExitReadLock();
    }
}