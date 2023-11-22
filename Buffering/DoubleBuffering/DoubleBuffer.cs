using System.Diagnostics;
using Buffering.BufferResources;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A type of buffer that minimizes locking times during front buffer updates.
/// The back buffer should be updated concurrently with a back buffer controller.
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public class DoubleBuffer<T, TUpdaterState>
    where T : struct
{
    // originally from array but remove extra pointer deref
    private BufferResource<T, TUpdaterState> _rsc0; // front
    private BufferResource<T, TUpdaterState> _rsc1; // back
    private BufferedResourceInfo _frontInfo;
    private readonly DoubleBufferConfiguration _config;

    /// <summary>
    /// Used to create a local value to read the front buffer from.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferFrontReader<T, TUpdaterState> FrontReader => new(this);
    /// <summary>
    /// Used to create a local value to update and swap the back buffer.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferBackController<T, TUpdaterState> BackController => new(this);

    /// <summary>
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="rsc">Copied to the two buffers to avoid issues with resource objects referencing the same existing object</param>
    /// <param name="configuration">Sets up how the double buffer will run. If null, uses default configuration</param>
    public DoubleBuffer(in BufferResource<T, TUpdaterState> rsc, DoubleBufferConfiguration? configuration = null)
    {
        _config = configuration ?? DoubleBufferConfiguration.Default;
        _rsc0 = new BufferResource<T, TUpdaterState>(rsc);
        _rsc1 = new BufferResource<T, TUpdaterState>(rsc);
        _frontInfo = default;
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
        var hlock = _rsc0.Lock(ResourceAccessFlags.Read);
        rsc = _rsc0.Resource;
        info = _frontInfo;
        return hlock;
    }

    /// <summary>
    /// Updates the back buffer by updating the resource.
    /// Should be called before swapping the buffers and on a dedicated back buffer thread
    /// to maximize throughput.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done.
    /// </summary>
    internal void UpdateBackBuffer(TUpdaterState state)
    {
        _rsc1.UpdateResource(state);
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
                var hlock1 = _rsc0.Lock(ResourceAccessFlags.Write);
                var t = _rsc0;
                _rsc0 = _rsc1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                _rsc1 = t;
                break;
            
            case DoubleBufferSwapEffect.Copy:
                var hlock2 = _rsc0.Lock(ResourceAccessFlags.Write);
                _rsc0.CopyFromResource(_rsc1);
                _frontInfo = nextInfo;
                hlock2.Dispose();
                break;
            
            default:
                throw new NotSupportedException(
                    "Unsupported swap effect");
        }
        
        Debug.Assert(!ReferenceEquals(_rsc0, _rsc1));
    }
}