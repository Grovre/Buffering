﻿using Buffering.DoubleBuffering;
using Buffering.Locking;

namespace Buffering.SingleBuffering;

public class SingleBuffer<T>
    where T : struct
{
    private readonly BufferingResource<T> _rsc;
    private ResourceInfo _info;
    private readonly SingleBufferConfiguration _config;

    public SingleBuffer(BufferingResource<T> rsc, SingleBufferConfiguration? config = null)
    {
        config ??= new SingleBufferConfiguration();
        _rsc = new(rsc);
        _config = config;
        _info = default;
    }

    public ResourceLockHandle ReadBuffer(out T rsc, out ResourceInfo info)
    {
        var hlock = _rsc.Lock(ResourceAccessFlag.Read);
        rsc = _rsc.Resource;
        info = _info;
        return hlock;
    }

    public void UpdateBuffer()
    {
        using var hlock = _rsc.Lock(ResourceAccessFlag.Read);
        _rsc.UpdateResource();
        _info = ResourceInfo.PrepareNextInfo(_info, true);
    }
}