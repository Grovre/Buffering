using System.Numerics;

namespace Buffering.DoubleBuffering;

/// <summary>
/// Used to control the back buffer of a double buffer
/// </summary>
/// <typeparam name="T">The type in the double buffer</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public readonly struct DoubleBufferBackController<T, TUpdaterState>
    where T : struct
{
    private readonly DoubleBuffer<T, TUpdaterState> _doubleBuffer;
    
    /// <summary>
    /// Should be used to retrieve a double buffer,
    /// preferably through the double buffer itself
    /// </summary>
    /// <param name="doubleBuffer">DoubleBuffer to control</param>
    public DoubleBufferBackController(DoubleBuffer<T, TUpdaterState> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
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
        _doubleBuffer.UpdateBackBuffer(state);
    }

    /// <inheritdoc cref="M:Buffering.DoubleBuffering.DoubleBuffer`1.SwapBuffers"/>
    public void SwapBuffers()
    {
        _doubleBuffer.SwapBuffers();
    }
}