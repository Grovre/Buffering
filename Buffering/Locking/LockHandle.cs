using System.Diagnostics;

namespace Buffering.Locking;

public ref struct LockHandle
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

    public readonly void Dispose()
    {
        Owner.Unlock(this);
    }
}