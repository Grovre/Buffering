using System.Runtime.CompilerServices;
using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffers;

public class DoubleBuffer<T>
{
    private BufferingResource<T>[] _resources = new BufferingResource<T>[2];
    private IBufferLock _lock;

    public DoubleBuffer(BufferingResource<T> rsc, DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= new DoubleBufferConfiguration();
        _resources[0] = rsc;
        _resources[1] = rsc;
        _lock = new MultipleReaderLock();
    }

    // Handle can be immediately disposed if T : struct
    public LockHandle ReadFrontBuffer(out T rsc)
    {
        var hlock = _lock.Lock(BufferAccessFlag.Read);
        rsc = _resources[0].Resource;
        return hlock;
    }

    public void UpdateBackBuffer()
    {
        _resources[1].UpdateResource();
    }

    public void SwapBuffers()
    {
        var hlock = _lock.Lock(BufferAccessFlag.Write);
        var t = _resources[0];
        _resources[0] = _resources[1];
        hlock.Dispose();
        _resources[1] = t;
    }
}