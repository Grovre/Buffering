using System.Numerics;
using Buffering.Locking;

namespace Buffering.DoubleBuffers;

public class DoubleBuffer<T>
{
    private BufferingResource<T>[] _resources = new BufferingResource<T>[2];
    private readonly IBufferLock _lock;
    private ResourceInfo _frontInfo;

    public DoubleBuffer(BufferingResource<T> rsc, DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= DoubleBufferConfiguration.Default;
        _lock = configuration.LockImpl;
        _resources[0] = rsc;
        _resources[1] = rsc;
        _frontInfo = new();
    }

    // Handle can be immediately disposed if T : struct
    public LockHandle ReadFrontBuffer(out T rsc, out ResourceInfo info)
    {
        var hlock = _lock.Lock(BufferAccessFlag.Read);
        rsc = _resources[0].Resource;
        info = _frontInfo;
        return hlock;
    }

    public void UpdateBackBuffer()
    {
        _resources[1].UpdateResource();
    }
    
    public void SwapBuffers()
    {
        var nextInfo = PrepareNextInfoOnSwap();
        var hlock = _lock.Lock(BufferAccessFlag.Write);
        var t = _resources[0];
        _resources[0] = _resources[1];
        _frontInfo = nextInfo;
        hlock.Dispose(); // Quick release
        _resources[1] = t;
    }

    private ResourceInfo PrepareNextInfoOnSwap()
    {
        var id = _frontInfo.Id;
        id += 1;
        
        return new ResourceInfo(id, byte.MaxValue);
    }
}