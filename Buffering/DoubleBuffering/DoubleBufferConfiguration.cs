using Buffering.BufferResources;
using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffering;

/// <summary>
/// Configuration class for determining functionality of a double buffer
/// </summary>
public class DoubleBufferConfiguration : BufferConfiguration
{
    /// <summary>
    /// Swap effect to be used with the double buffer
    /// </summary>
    public DoubleBufferSwapEffect SwapEffect { get; set; }
    /// <summary>
    /// Type of resource lock to be used with the double buffer
    /// </summary>
    public IResourceLock ResourceLock { get; set; }
    
    public DoubleBufferConfiguration(DoubleBufferSwapEffect swapEffect, IResourceLock resourceLockType)
    {
        SwapEffect = swapEffect;
        ResourceLock = resourceLockType;
    }
}