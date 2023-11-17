namespace Buffering.Locking
{
    /// <summary>
    /// Represents a lock implementation for a buffer
    /// </summary>
    public interface IBufferLock
    {
        internal const string BadOwnerExceptionMessage = "Lock handle not owned by lock queried for unlocking";
        
        internal LockHandle Lock(BufferAccessFlag flags = BufferAccessFlag.Generic);

        internal void Unlock(LockHandle hlock);
    }
}