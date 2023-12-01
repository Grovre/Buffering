using Buffering.BufferResources;
using Buffering.DoubleBuffering;
using Buffering.Locking;

namespace Buffering.SingleBuffering;

/// <summary>
/// A single buffer where updating and reading happens in the same buffer.
/// This means that updating the buffer can lead to locks. To avoid this,
/// use a multiple-buffering solution like double buffering where the
/// front buffer is locked only for reading and pushing an updated buffer
/// to the front. This minimizes lock times and updating happens concurrently.
/// </summary>
/// <typeparam name="T">Type of value represented by the buffer's resource</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public class SingleBuffer<T, TUpdaterState>
    where T : struct
{
    private readonly BufferResource<T, TUpdaterState> _rsc;
    private BufferedResourceInfo _info;
    private readonly SingleBufferConfiguration _config;

    /// <summary>
    /// Constructs a single buffer using the provided resource and configuration.
    /// </summary>
    /// <param name="rsc">Resource to use as a buffer</param>
    /// <param name="config">How the single buffer should be configured for use</param>
    public SingleBuffer(BufferResource<T, TUpdaterState> rsc, SingleBufferConfiguration? config = null)
    {
        config ??= new SingleBufferConfiguration();
        _rsc = rsc;
        _config = config;
        _info = default;
    }

    /// <summary>
    /// Locks and reads the buffer
    /// </summary>
    /// <param name="rsc">The value in the buffer</param>
    /// <param name="info">Information unique to the current buffer frame</param>
    /// <returns>A lock handle used to unlock the resource</returns>
    public ResourceLockHandle ReadBuffer(out T rsc, out BufferedResourceInfo info)
    {
        var hlock = _rsc.Lock(ResourceAccessFlags.Read);
        rsc = _rsc.Resource;
        info = _info;
        return hlock;
    }
    
    /// <summary>
    /// Updates the buffer using a lock
    /// </summary>
    /// <param name="state"></param>
    public void UpdateBuffer(TUpdaterState state)
    {
        using var hlock = _rsc.Lock(ResourceAccessFlags.Read);
        _rsc.UpdateResource(state);
        _info = BufferedResourceInfo.PrepareNextInfo(_info, true);
    }
}