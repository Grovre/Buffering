using System.Numerics;

namespace Buffering.DoubleBuffering;

/// <summary>
/// Used to control the back buffer of a double buffer
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
public readonly struct DoubleBufferBackController<T, TUpdaterState>
    where T : struct
{
    /// <summary>
    /// The double buffer this is controlling
    /// </summary>
    public DoubleBuffer<T, TUpdaterState> DoubleBuffer { get; }
    
    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public DoubleBufferBackController(DoubleBuffer<T, TUpdaterState> doubleBuffer)
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

    /// <inheritdoc cref="M:Buffering.DoubleBuffering.DoubleBuffer`1.UpdateBackBuffer"/>
    public void UpdateBackBuffer(TUpdaterState state)
    {
        DoubleBuffer.UpdateBackBuffer(state);
    }

    /// <inheritdoc cref="M:Buffering.DoubleBuffering.DoubleBuffer`1.SwapBuffers"/>
    public void SwapBuffers()
    {
        DoubleBuffer.SwapBuffers();
    }
}