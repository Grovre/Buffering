namespace Buffering.Locking
{
    /// <summary>
    /// Represents a lock implementation for a buffer
    /// </summary>
    public interface IResourceLock
    {
        internal const string BadOwnerExceptionMessage = "Lock handle not owned by lock queried for unlocking";
        
        internal ResourceLockHandle Lock(ResourceAccessFlag flags);

        internal bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock);

        internal void Unlock(ResourceLockHandle hlock);

        internal IResourceLock Copy();
    }
}