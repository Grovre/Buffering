using System.Runtime.CompilerServices;
using Buffering.BufferResources;
using Buffering.Locking;
using Buffering.Locking.Locks;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A type of buffer that minimizes locking times during front buffer updates.
/// The back buffer should be updated concurrently with a back buffer controller.
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class DoubleBuffer<T>
{
    // originally from array but remove extra pointer deref
    private StrongBox<T> _rsc0; // front
    private StrongBox<T> _rsc1; // back
    private BufferedResourceInfo _frontInfo;
    private readonly DoubleBufferConfiguration _config;
    private readonly IResourceLock _lock;

    /// <summary>
    /// Used to create a local value to read the front buffer from.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferFrontReader<T> FrontReader => new(this);
    /// <summary>
    /// Used to create a local value to update and swap the back buffer.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferBackController<T> BackController => new(this);

    /// <summary>
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="configuration">Sets up how the double buffer will run. If null, uses default configuration</param>
    public DoubleBuffer(DoubleBufferConfiguration? configuration = null)
    {
        _config = configuration ?? new DoubleBufferConfiguration(DoubleBufferSwapEffect.Flip, new NoLock());
        _rsc0 = new();
        _rsc1 = new();
        _frontInfo = default;
        _lock = _config.ResourceLock;
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
    }
    
    /// <summary>
    /// Swaps the buffers with functionality according to the configured swap effect (default is flip).
    /// Should be called after updating the back buffer.
    /// All reads immediately after every swap are on the correct resource in the front buffer.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done. Must be called on a dedicated back buffer update thread.
    /// This maximizes throughput anyways.
    /// </summary>
    /// <exception cref="Exception">Unknown/unsupported swap effect</exception>
    internal void SwapBuffers()
    {
        var nextInfo = BufferedResourceInfo.PrepareNextInfo(_frontInfo, true);
        
        switch (_config.SwapEffect)
        {
            case DoubleBufferSwapEffect.Flip:
                var hlock1 = _lock.Lock(ResourceAccessFlags.Write);
                var t = _rsc0;
                _rsc0 = _rsc1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                _rsc1 = t;
                break;
            
            default:
                throw new NotSupportedException(
                    "Unsupported swap effect");
        }
    }
}