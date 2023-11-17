namespace Buffering.Locking.Locks;

public class SpinnerLock : IBufferLock
{
    private SpinLock _lock = new(false);
    
    public LockHandle Lock(BufferAccessFlag flags = BufferAccessFlag.Generic)
    {
        var lockTaken = false;
        _lock.TryEnter(10_000, ref lockTaken);
        return new LockHandle(this, BufferAccessFlag.Generic);
    }

    public void Unlock(LockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new Exception(IBufferLock.BadOwnerExceptionMessage);

        _lock.Exit();
    }
}