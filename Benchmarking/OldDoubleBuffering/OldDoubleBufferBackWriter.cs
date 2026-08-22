namespace Benchmarking.OldDoubleBuffering;

/// <summary>
/// Used to control the back buffer of an old double buffer (commit 4a9c72b4).
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
public readonly struct OldDoubleBufferBackWriter<T>
{
    private readonly OldDoubleBuffer<T> _doubleBuffer;

    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public OldDoubleBufferBackWriter(OldDoubleBuffer<T> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Should never be called. Retrieve through a double buffer
    /// </summary>
    /// <exception cref="NotImplementedException">When invoked</exception>
    public OldDoubleBufferBackWriter()
    {
        throw new NotImplementedException(
            "Back controller must be retrieved through a double buffer.");
    }

    /// <summary>
    /// Updates the back buffer.
    /// </summary>
    public void UpdateBackBuffer(in T value)
    {
        _doubleBuffer.UpdateBackBuffer(value);
    }

    /// <summary>
    /// Reads the back buffer.
    /// </summary>
    public ref T ReadBackBuffer()
    {
        return ref _doubleBuffer.ReadBackBuffer();
    }

    /// <summary>
    /// Swaps the buffers.
    /// </summary>
    public void SwapBuffers()
    {
        _doubleBuffer.SwapBuffers();
    }
}
