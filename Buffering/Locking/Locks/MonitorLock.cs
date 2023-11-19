using System.Security.Authentication;

namespace Buffering.Locking.Locks;

/// <summary>
/// Uses a monitor to lock the front buffer for reading/writing.
/// Best use case is for two threads where one is reading the buffer and the other is updating the buffer.
/// BufferAccessFlag is ignored as only one thread can enter the monitor at a time.
/// </summary>
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
            throw new AuthenticationException(IBufferLock.BadOwnerExceptionMessage);
        
        Monitor.Exit(this);
    }
}