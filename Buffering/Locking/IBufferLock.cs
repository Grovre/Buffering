using System.Buffers;

namespace Buffering.Locking
{
    public interface IBufferLock
    {
        internal static Stack<LockHandle> HandleStackPool = new();
        
        LockHandle Lock(Accessor accessor = 0);

        internal void Unlock(LockHandle lhnd);
    }
}