using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Buffering.Locking;

namespace Buffering.DoubleBuffers;

public class DoubleBuffer<T>
    where T : struct
{
    // originally from array but remove extra pointer deref
    private BufferingResource<T> _rsc0; // front
    private BufferingResource<T> _rsc1; // back
    private readonly IBufferLock _lock;
    private ResourceInfo _frontInfo;
    private DoubleBufferSwapEffect _swapEffect;

    public DoubleBuffer(BufferingResource<T> rsc, DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= DoubleBufferConfiguration.Default;
        _lock = configuration.LockImpl;
        _rsc0 = new BufferingResource<T>(rsc);
        _rsc1 = new BufferingResource<T>(rsc);
        _frontInfo = default;
        _swapEffect = configuration.SwapEffect;
    }

    // Handle can be immediately disposed if T : struct
    public LockHandle ReadFrontBuffer(out T rsc, out ResourceInfo info)
    {
        var hlock = _lock.Lock(BufferAccessFlag.Read);
        rsc = _rsc0.Resource;
        info = _frontInfo;
        return hlock;
    }

    public void UpdateBackBuffer()
    {
        _rsc1.UpdateResource();
    }
    
    public void SwapBuffers()
    {
        var nextInfo = PrepareNextInfoOnSwap();

        switch (_swapEffect)
        {
            case DoubleBufferSwapEffect.Flip:
                var hlock1 = _lock.Lock(BufferAccessFlag.Write);
                var t = _rsc0;
                _rsc0 = _rsc1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                _rsc1 = t;
                break;
            
            case DoubleBufferSwapEffect.Copy:
                if (BufferingResource<T>.ResourceIsOrContainsReferences)
                    throw new Exception(
                        "Cannot do a copying swap effect when T is a reference or contains references");

                var hlock3 = _lock.Lock(BufferAccessFlag.Write);
                _rsc0.CopyFromResource(_rsc1);
                _frontInfo = nextInfo;
                hlock3.Dispose();
                break;
            
            default:
                throw new Exception("Unsupported swap effect");
        }
        
        Debug.Assert(!ReferenceEquals(_rsc0, _rsc1));
    }

    private ResourceInfo PrepareNextInfoOnSwap()
    {
        const bool fromBuffer = true;
        var id = unchecked(_frontInfo.Id + 1);
        
        return new ResourceInfo(id, fromBuffer);
    }
}