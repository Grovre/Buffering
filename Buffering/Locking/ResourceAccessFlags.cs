namespace Buffering.Locking;

/// <summary>
/// Different modes of access to a resource
/// </summary>
[Flags]
public enum ResourceAccessFlags
{
    /// <summary>
    /// Useful when locking is read/write-agnostic.
    /// For example, monitor locking doesn't care how
    /// a resource is being used, just that one thread
    /// accesses at a time.
    /// </summary>
    Generic = 1 << 0,
    /// <summary>
    /// The intent of the locking thread is to read the resource.
    /// Useful for locking with a MultipleReaderLock
    /// </summary>
    Read = 1 << 1,
    /// <summary>
    /// The intent of the locking thread is to write to the resource.
    /// Useful for locking with a MultipleReaderLock.
    /// </summary>
    Write = 1 << 2
}