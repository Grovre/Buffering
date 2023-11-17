namespace Buffering.Locking.Locks;

public class MonitorLock : IBufferLock
{
    public LockHandle Lock(BufferAccessFlag flags = BufferAccessFlag.Generic)
    {
        Monitor.Enter(this);
        return new LockHandle(this, BufferAccessFlag.Generic);
    }

    public void Unlock(LockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new Exception(IBufferLock.BadOwnerExceptionMessage);
        Monitor.Exit(this);
    }
}