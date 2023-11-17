namespace Buffering.Locking;

/// <summary>
/// Represents a lock entered in a buffer.
/// Must be disposed of IMMEDIATELY after reading or writing to the buffer.
/// If not disposed, the buffer will be locked.
/// Readonly ref struct guarantees stack allocation for fastest locking and unlocking in an OOP manner.
/// </summary>
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

    /// <summary>
    /// Must be called immediately after reading or writing to the buffer the lock represents
    /// </summary>
    public void Dispose()
    {
        Owner.Unlock(this);
    }
}