using System.Diagnostics;

namespace Buffering.Locking;

public readonly ref struct LockHandle
{
    internal IBufferLock Owner { get; }
    public BufferAccessFlag AccessFlag { get; }

    internal LockHandle(IBufferLock owner, BufferAccessFlag accessFlag)
    {
        Owner = owner;
        AccessFlag = accessFlag;
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