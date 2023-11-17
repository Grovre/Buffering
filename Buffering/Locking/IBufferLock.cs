using System.Buffers;

namespace Buffering.Locking
{
    public interface IBufferLock
    {
        internal LockHandle Lock(BufferAccessFlag flags = BufferAccessFlag.Generic);

        internal void Unlock(LockHandle lhnd);
    }
}