using Buffering.Locking;

namespace Buffering.SingleBuffering;

public class SingleBufferConfiguration : BufferConfiguration
{
    public SingleBufferConfiguration(IBufferLock lockImpl) : base(lockImpl)
    {
    }
}