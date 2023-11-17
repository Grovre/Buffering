namespace Buffering.Locking;

public readonly ref struct LockHandle
{
    internal IBufferLock Owner { get; }
    public BufferAccessFlag AccessFlags { get; }

    internal LockHandle(IBufferLock owner, BufferAccessFlag accessFlags)
    {
        Owner = owner;
        AccessFlags = accessFlags;
    }

    public LockHandle()
    {
        // TODO: NoLock
    }

    public void Dispose()
    {
        Owner.Unlock(this);
    }
}