using Buffering.BufferResources;
using Buffering.DoubleBuffering;
using Buffering.Locking;

namespace Buffering.SingleBuffering;

public class SingleBuffer<T, TUpdaterState>
    where T : struct
{
    private readonly BufferResource<T, TUpdaterState> _rsc;
    private BufferedResourceInfo _info;
    private readonly SingleBufferConfiguration _config;

    public SingleBuffer(BufferResource<T, TUpdaterState> rsc, SingleBufferConfiguration? config = null)
    {
        config ??= new SingleBufferConfiguration();
        _rsc = new(rsc);
        _config = config;
        _info = default;
    }

    public ResourceLockHandle ReadBuffer(out T rsc, out BufferedResourceInfo info)
    {
        var hlock = _rsc.Lock(ResourceAccessFlag.Read);
        rsc = _rsc.Resource;
        info = _info;
        return hlock;
    }

    public void UpdateBuffer(TUpdaterState state)
    {
        using var hlock = _rsc.Lock(ResourceAccessFlag.Read);
        _rsc.UpdaterState = state;
        _rsc.UpdateResource();
        _info = BufferedResourceInfo.PrepareNextInfo(_info, true);
    }
}