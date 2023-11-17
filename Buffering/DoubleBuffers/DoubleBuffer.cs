using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Buffering.Locking;

namespace Buffering.DoubleBuffers;

public class DoubleBuffer<T>
    where T : struct
{
    private BufferingResource<T>[] _resources = new BufferingResource<T>[2];
    private readonly IBufferLock _lock;
    private ResourceInfo _frontInfo;
    private DoubleBufferSwapEffect _swapEffect;

    public DoubleBuffer(BufferingResource<T> rsc, DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= DoubleBufferConfiguration.Default;
        _lock = configuration.LockImpl;
        _resources[0] = new BufferingResource<T>(rsc);
        _resources[1] = new BufferingResource<T>(rsc);
        _frontInfo = default;
        _swapEffect = configuration.SwapEffect;
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
        ref var v0 = ref _resources[0];
        ref var v1 = ref _resources[1];
        
        switch (_swapEffect)
        {
            case DoubleBufferSwapEffect.Flip:
                var hlock1 = _lock.Lock(BufferAccessFlag.Write);
                var t = v0;
                v0 = v1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                v1 = t;
                break;
            
            case DoubleBufferSwapEffect.Copy:
                if (BufferingResource<T>.ResourceIsOrContainsReferences)
                    throw new Exception(
                        "Cannot do a copying swap effect when T is a reference or contains references");

                var hlock3 = _lock.Lock(BufferAccessFlag.Write);
                v0.CopyFromResource(v1);
                _frontInfo = nextInfo;
                hlock3.Dispose();
                break;
            
            default:
                throw new Exception("Unsupported swap effect");
        }
        
        Debug.Assert(!ReferenceEquals(_resources[0], _resources[1]));
    }

    private ResourceInfo PrepareNextInfoOnSwap()
    {
        const bool fromBuffer = true;
        var id = unchecked(_frontInfo.Id + 1);
        
        return new ResourceInfo(id, fromBuffer);
    }
}