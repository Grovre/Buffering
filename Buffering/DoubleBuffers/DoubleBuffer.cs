using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffers;

public ref struct DoubleBuffer<T>
    where T : struct
{
    private BufferingResource<T> _front;
    private BufferingResource<T> _back;
    private readonly IBufferLock _lock;

    internal DoubleBuffer(DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= new DoubleBufferConfiguration();
        _lock = new MultipleReaderLock();
    }

    public void UpdateBackBuffer()
    {
        _back.UpdateResource();
    }

    internal void SwapBuffers()
    {
        // TODO: Do more than just a discarding swap effect
        using var lhnd = _lock.Lock(BufferAccessFlag.Write);
        (_front, _back) = (_back, _front);
        lhnd.Dispose();
    }

    public LockHandle ReadFrontBuffer(out T buffer)
    {
        var lhnd = _lock.Lock(BufferAccessFlag.Read);
        buffer = _front.Resource;
        return lhnd;
    }
}