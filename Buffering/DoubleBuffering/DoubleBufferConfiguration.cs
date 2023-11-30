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
    public DoubleBufferSwapEffect SwapEffect { get; }
    
    public DoubleBufferConfiguration(DoubleBufferSwapEffect swapEffect)
    {
        SwapEffect = swapEffect;
    }
}