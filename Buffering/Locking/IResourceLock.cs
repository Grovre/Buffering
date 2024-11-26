using System.Security.Authentication;

namespace Buffering.Locking
{
    // TODO: NoLock
    
    /// <summary>
    /// Represents a lock implementation for a buffer
    /// </summary>
    public interface IResourceLock
    {
        internal const string BadOwnerExceptionMessage = "Lock handle not owned by lock queried for unlocking";
        
        /// <summary>
        /// Locks onto the resource and returns a handle to unlock with
        /// </summary>
        /// <param name="flags">The intent of locking onto the resource. Useful when configured with MultipleReaderLock</param>
        /// <returns>A disposable handle to unlock with</returns>
        internal ResourceLockHandle Lock(ResourceAccessFlags flags);

        /// <summary>
        /// Attempts to lock onto the resource
        /// </summary>
        /// <param name="flags">The intent of locking onto the resource. Useful when configured with MultipleReaderLock</param>
        /// <param name="hlock">A disposable handle to unlock with</param>
        /// <returns>Whether or not the lock was successful</returns>
        internal bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock);

        /// <summary>
        /// Unlocks the resource
        /// </summary>
        /// <param name="hlock">Lock handle used and managed by the resource lock</param>
        /// <exception cref="AuthenticationException">Thrown when the handle came from a different resource lock other than the provider</exception>
        internal void Unlock(ResourceLockHandle hlock);

        internal IResourceLock Copy();
    }
}