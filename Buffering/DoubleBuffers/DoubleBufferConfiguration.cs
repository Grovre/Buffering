using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffers;

public class DoubleBufferConfiguration
{
    public static DoubleBufferConfiguration Default
        => new(new MultipleReaderLock());
    
    public IBufferLock LockImpl { get; set; }

    public DoubleBufferConfiguration(IBufferLock lockImpl)
    {
        LockImpl = lockImpl;
    }
}