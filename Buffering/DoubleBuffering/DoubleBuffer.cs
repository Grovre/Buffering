using System.Diagnostics;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A double buffer
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class DoubleBuffer<T>
    where T : struct
{
    // originally from array but remove extra pointer deref
    private BufferingResource<T> _rsc0; // front
    private BufferingResource<T> _rsc1; // back
    private readonly IBufferLock _lock;
    private ResourceInfo _frontInfo;
    private readonly DoubleBufferSwapEffect _swapEffect;

    public DoubleBufferFrontReader<T> FrontReader => new(this);
    public DoubleBufferBackController<T> BackController => new(this);

    /// <summary>
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="rsc">Copied to the two buffers to avoid issues with resource objects referencing the same existing object</param>
    /// <param name="configuration">Sets up how the double buffer will run. If null, uses default configuration</param>
    public DoubleBuffer(in BufferingResource<T> rsc, DoubleBufferConfiguration? configuration = null)
    {
        configuration ??= DoubleBufferConfiguration.Default;
        _lock = configuration.LockImpl;
        _rsc0 = new BufferingResource<T>(rsc);
        _rsc1 = new BufferingResource<T>(rsc);
        _frontInfo = default;
        _swapEffect = configuration.SwapEffect;
    }
    
    /// <summary>
    /// Locks the front buffer and reads it.
    /// The lock should be immediately disposed of in the same statement if T is a struct and contains no references
    /// </summary>
    /// <param name="rsc">Ref variable to read the buffer to</param>
    /// <param name="info">Minimal information about the current front buffer object</param>
    /// <returns>LockHandle to be disposed of immediately after reading/writing the buffer. This should be done ASAP</returns>
    internal LockHandle ReadFrontBuffer(out T rsc, out ResourceInfo info)
    {
        var hlock = _lock.Lock(BufferAccessFlag.Read);
        rsc = _rsc0.Resource;
        info = _frontInfo;
        return hlock;
    }

    /// <summary>
    /// Updates the back buffer by updating the resource.
    /// Should be called before swapping the buffers and on a dedicated back buffer thread
    /// to maximize throughput.
    /// The back buffer IS NOT THREADSAFE. No locking or synchronization is done.
    /// </summary>
    internal void UpdateBackBuffer()
    {
        _rsc1.UpdateResource();
    }
    
    /// <summary>
    /// Swaps the buffers with functionality according to the configured swap effect (default is flip).
    /// Should be called after updating the back buffer.
    /// All reads immediately after every swap are on the correct resource in the front buffer.
    /// The back buffer IS NOT THREADSAFE. No locking or synchronization is done. Must be called on a dedicated back buffer update thread.
    /// This maximizes throughput anyways.
    /// </summary>
    /// <exception cref="Exception">Unknown/unsupported swap effect</exception>
    internal void SwapBuffers()
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
                var hlock2 = _lock.Lock(BufferAccessFlag.Write);
                _rsc0.CopyFromResource(_rsc1);
                _frontInfo = nextInfo;
                hlock2.Dispose();
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