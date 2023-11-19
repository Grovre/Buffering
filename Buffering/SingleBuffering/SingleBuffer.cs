using Buffering.DoubleBuffering;
using Buffering.Locking;

namespace Buffering.SingleBuffering;

public class SingleBuffer<T>
    where T : struct
{
    private readonly BufferingResource<T> _rsc;
    private ResourceInfo _info;
    private readonly SingleBufferConfiguration _config;

    public SingleBuffer(BufferingResource<T> rsc, SingleBufferConfiguration config)
    {
        _rsc = new(rsc);
        _config = config;
        _info = default;
    }

    public LockHandle ReadBuffer(out T rsc, out ResourceInfo info)
    {
        var hlock = _config.LockImpl.Lock(BufferAccessFlag.Read);
        rsc = _rsc.Resource;
        info = _info;
        return hlock;
    }

    public void UpdateBuffer()
    {
        using var hlock = _config.LockImpl.Lock(BufferAccessFlag.Read);
        _rsc.UpdateResource();
        _info = ResourceInfo.PrepareNextInfo(_info, true);
    }

    /// <summary>
    /// If this is needed, look into double buffering.
    /// Only implemented for the one-off chance this may
    /// be needed.
    /// </summary>
    public void UpdateBufferSeparately()
    {
        var hlock = _config.LockImpl.Lock(BufferAccessFlag.Read);
        var rsc = new BufferingResource<T>(_rsc);
        hlock.Dispose();
        
        rsc.UpdateResource();
        hlock = _config.LockImpl.Lock(BufferAccessFlag.Write);
        _rsc.CopyFromResource(rsc);
        hlock.Dispose();
    }
}