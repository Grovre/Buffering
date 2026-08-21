namespace Buffering.DoubleBuffering;

/// <summary>
/// Used to read the front buffer of a double buffer
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
public readonly struct DoubleBufferFrontReader<T>
{
    private readonly DoubleBuffer<T> _doubleBuffer;

    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public DoubleBufferFrontReader(DoubleBuffer<T> doubleBuffer)
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

    /// <summary>
    /// Reads the front buffer without locking.
    /// </summary>
    /// <returns>The front buffer value</returns>
    public T ReadFrontBuffer()
    {
        return _doubleBuffer.ReadFrontBuffer();
    }
}