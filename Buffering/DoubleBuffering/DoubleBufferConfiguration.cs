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
        => new(new MultipleReaderLock(), DoubleBufferSwapEffect.Flip);
    
    /// <summary>
    /// Swap effect to be used with the double buffer
    /// </summary>
    public DoubleBufferSwapEffect SwapEffect { get; }
    
    public DoubleBufferConfiguration(IBufferLock lockImpl, DoubleBufferSwapEffect swapEffect) : base(lockImpl)
    {
        SwapEffect = swapEffect;
    }
}