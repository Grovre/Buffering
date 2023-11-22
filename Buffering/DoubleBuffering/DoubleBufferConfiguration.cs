using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffering;

/// <summary>
/// Configuration class for determining functionality of a double buffer
/// </summary>
public class DoubleBufferConfiguration : BufferConfiguration
{
    /// <summary>
    ///  Default configuration.
    /// Uses a MultipleReaderLock in case more than one thread is reading the front buffer.
    /// Uses the flip swap effect to minimize swap time and that structs do not need to be copied in most cases.
    /// </summary>
    public static DoubleBufferConfiguration Default
        => new(DoubleBufferSwapEffect.Flip);
    
    /// <summary>
    /// Swap effect to be used with the double buffer <see cref="DoubleBufferSwapEffect"/>
    /// </summary>
    public DoubleBufferSwapEffect SwapEffect { get; }
    
    /// <summary>
    /// Allargs constructor for making a configuration
    /// </summary>
    /// <param name="swapEffect">The swap effect to use</param>
    public DoubleBufferConfiguration(DoubleBufferSwapEffect swapEffect)
    {
        SwapEffect = swapEffect;
    }
}