using Buffering.Locking;

namespace Buffering;

/// <summary>
/// Abstract type that all buffer configurations are based off of.
/// Includes the locking implementation for the buffers this is used on.
/// </summary>
public abstract class BufferConfiguration
{
    /// <summary>
    /// Lock implementation to be used with the double buffer
    /// </summary>
    public IBufferLock LockImpl { get; }
    
    protected BufferConfiguration(IBufferLock lockImpl)
    {
        LockImpl = lockImpl;
    }
}