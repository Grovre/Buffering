using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffers;

public class DoubleBufferConfiguration
{
    public static DoubleBufferConfiguration Default
        => new(new MultipleReaderLock(), DoubleBufferSwapEffect.Flip);
    
    public IBufferLock LockImpl { get; }
    public DoubleBufferSwapEffect SwapEffect { get; }

    public DoubleBufferConfiguration(IBufferLock lockImpl, DoubleBufferSwapEffect swapEffect)
    {
        LockImpl = lockImpl;
        SwapEffect = swapEffect;
    }
}