using System.Runtime.CompilerServices;
using Buffering.BufferResources;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A type of buffer that minimizes locking times during front buffer updates.
/// The back buffer should be updated concurrently with a back buffer controller.
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class DoubleBuffer<T>
{
    private StrongBox<T> _rsc0; // front
    private bool _frontUpdated = false;
    private StrongBox<T> _rsc1; // back
    private bool _backUpdated = false;
    private BufferedResourceInfo _frontInfo;
    private readonly IResourceLock _lock;
    private readonly DoubleBufferSwapEffect _swapEffect;

    /// <summary>
    /// Used to create a local value to read the front buffer from.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferFrontReader<T> FrontReader => new(this);
    /// <summary>
    /// Used to create a local value to update and swap the back buffer.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferBackWriter<T> BackWriter => new(this);

    /// <summary>
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="lockImpl">Lock implementation to use</param>
    /// <param name="swapEffect">Swap effect to use</param>
    public DoubleBuffer(IResourceLock lockImpl, DoubleBufferSwapEffect swapEffect)
    {
        _rsc0 = new();
        _rsc1 = new();
        _frontInfo = default;
        _lock = lockImpl.Copy();
        _swapEffect = swapEffect;
    }
    
    /// <summary>
    /// Locks the front buffer and reads it.
    /// The lock should be immediately disposed of in the same statement if T is a struct and contains no references
    /// </summary>
    /// <param name="rsc">Ref variable to read the buffer to</param>
    /// <param name="info">Minimal information about the current front buffer object</param>
    /// <returns>ResourceLockHandle to be disposed of immediately after reading/writing the buffer. This should be done ASAP</returns>
    internal ResourceLockHandle ReadFrontBuffer(out T rsc, out BufferedResourceInfo info)
    {
        var hlock = _lock.Lock(ResourceAccessFlags.Read);
        rsc = _rsc0.Value!;
        info = _frontInfo;
        return hlock;
    }

    /// <summary>
    /// Updates the back buffer by updating the resource.
    /// Should be called before swapping the buffers and on a dedicated back buffer thread
    /// to maximize throughput.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done.
    /// </summary>
    internal void UpdateBackBuffer(in T value)
    {
        _rsc1.Value = value;
        _backUpdated = true;
    }
    /// <summary>
    /// Reads the back buffer and returns a reference to it.
    /// </summary>
    /// <returns>A reference to the back buffer</returns>
    /// <exception cref="NotSupportedException">When the front buffer has not ben initially set for a reference return</exception>
    internal ref T ReadBackBuffer()
    {
        if (!_frontUpdated || !_backUpdated)
            throw new NotSupportedException(
                "A buffer has not been initialized for a reference return");

        return ref _rsc1.Value!;
    }

    /// <summary>
    /// Swaps the buffers with functionality according to the configured swap effect (default is flip).
    /// Should be called after updating the back buffer.
    /// All reads immediately after every swap are on the correct resource in the front buffer.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done.
    /// This maximizes throughput out of the box.
    /// </summary>
    /// <exception cref="NotSupportedException">Unknown/unsupported swap effect</exception>
    internal void SwapBuffers()
    {
        var nextInfo = BufferedResourceInfo.PrepareNextInfo(_frontInfo, true);
        
        switch (_swapEffect)
        {
            case DoubleBufferSwapEffect.Flip:
                var t = _rsc0;
                var hlock1 = _lock.Lock(ResourceAccessFlags.Write);
                _rsc0 = _rsc1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                _rsc1 = t;

                (_backUpdated, _frontUpdated) = (_frontUpdated, _backUpdated);
                break;
            
            default:
                throw new NotSupportedException(
                    "Unsupported swap effect");
        }
    }
}