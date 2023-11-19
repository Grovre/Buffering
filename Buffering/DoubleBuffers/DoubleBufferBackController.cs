namespace Buffering.DoubleBuffers;

public readonly struct DoubleBufferBackController<T>
    where T : struct
{
    /// <summary>
    /// The double buffer this is controlling
    /// </summary>
    public DoubleBuffer<T> DoubleBuffer { get; }
    
    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public DoubleBufferBackController(DoubleBuffer<T> doubleBuffer)
    {
        DoubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Should never be called. Retrieve through a double buffer
    /// </summary>
    /// <exception cref="NotImplementedException">When invoked</exception>
    public DoubleBufferBackController()
    {
        throw new NotImplementedException(
            "Back controller must be retrieved through a double buffer.");
    }

    /// <inheritdoc cref="M:Buffering.DoubleBuffers.DoubleBuffer`1.UpdateBackBuffer"/>
    public void UpdateBackBuffer()
    {
        DoubleBuffer.UpdateBackBuffer();
    }

    /// <inheritdoc cref="M:Buffering.DoubleBuffers.DoubleBuffer`1.SwapBuffers"/>
    public void SwapBuffers()
    {
        DoubleBuffer.SwapBuffers();
    }
}