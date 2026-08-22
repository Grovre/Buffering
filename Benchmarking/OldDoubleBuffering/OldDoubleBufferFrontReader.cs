using Buffering.Locking;

namespace Benchmarking.OldDoubleBuffering;

/// <summary>
/// Used to read the front buffer of an old double buffer (commit 4a9c72b4).
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
public readonly struct OldDoubleBufferFrontReader<T>
{
    private readonly OldDoubleBuffer<T> _doubleBuffer;

    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public OldDoubleBufferFrontReader(OldDoubleBuffer<T> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Should never be called. Retrieve through a double buffer
    /// </summary>
    /// <exception cref="NotImplementedException">When invoked</exception>
    public OldDoubleBufferFrontReader()
    {
        throw new NotImplementedException(
            "Front reader must be retrieved through a double buffer.");
    }

    /// <summary>
    /// Reads the front buffer with lock handle.
    /// </summary>
    public ResourceLockHandle ReadFrontBuffer(out T rsc, out BufferedResourceInfo info)
    {
        return _doubleBuffer.ReadFrontBuffer(out rsc, out info);
    }
}
