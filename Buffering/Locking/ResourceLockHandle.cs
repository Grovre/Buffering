namespace Buffering.Locking;

/// <summary>
/// Represents a lock entered in a buffer.
/// Must be disposed of IMMEDIATELY after reading or writing to the buffer.
/// If not disposed, the buffer will be locked.
/// Readonly ref struct guarantees stack allocation for fastest locking and unlocking in an OOP manner.
/// </summary>
public readonly struct ResourceLockHandle : IDisposable
{
    internal IResourceLock Owner { get; }
    /// <summary>
    /// The access flags used to lock the resource with.
    /// </summary>
    public ResourceAccessFlags AccessFlags { get; }

    internal ResourceLockHandle(IResourceLock owner, ResourceAccessFlags accessFlags)
    {
        Owner = owner;
        AccessFlags = accessFlags;
    }

    /// <summary>
    /// Do not use.
    /// </summary>
    /// <exception cref="NotImplementedException">When invoked</exception>
    public ResourceLockHandle()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Must be called immediately after reading or writing to the buffer the lock represents
    /// </summary>
    public void Dispose()
    {
        Owner.Unlock(this);
    }
}