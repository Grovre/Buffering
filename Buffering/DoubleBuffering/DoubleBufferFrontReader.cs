using Buffering.BufferResources;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// Used to read the front buffer of a double buffer
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public readonly struct DoubleBufferFrontReader<T, TUpdaterState>
    where T : struct
{
    private readonly DoubleBuffer<T, TUpdaterState> _doubleBuffer;

    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public DoubleBufferFrontReader(DoubleBuffer<T, TUpdaterState> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Should never be called. Retrieve through a double buffer
    /// </summary>
    /// <exception cref="NotImplementedException">When invoked</exception>
    public DoubleBufferFrontReader()
    {
        throw new NotImplementedException(
            "Front reader must be retrieved through a double buffer.");
    }

    /// <inheritdoc cref="M:Buffering.DoubleBuffering.DoubleBuffer`1.ReadFrontBuffer(`0@,Buffering.DoubleBuffering.BufferedResourceInfo@)"/>
    public ResourceLockHandle ReadFrontBuffer(out T rsc, out BufferedResourceInfo info)
    {
        return _doubleBuffer.ReadFrontBuffer(out rsc, out info);
    }
}