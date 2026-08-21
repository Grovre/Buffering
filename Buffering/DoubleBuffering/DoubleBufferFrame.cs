namespace Buffering.DoubleBuffering;

/// <summary>
/// Represents a snapshot/frame of the front and back buffer states and whether the back buffer has a pending update.
/// </summary>
/// <typeparam name="T">The type of the buffered resources</typeparam>
/// <param name="front">Initial front buffer value</param>
/// <param name="back">Initial back buffer value</param>
/// <param name="hasPendingUpdate">Whether the back buffer has a pending update</param>
public class DoubleBufferFrame<T>(T front, T back, bool hasPendingUpdate = true)
{
    /// <summary>
    /// Gets the front buffer value for this frame.
    /// </summary>
    public T Front { get; internal set; } = front;

    /// <summary>
    /// Gets the back buffer value for this frame.
    /// </summary>
    public T Back { get; internal set; } = back;

    /// <summary>
    /// Gets a value indicating whether the back buffer contains a pending update that has not yet been swapped to the front buffer.
    /// </summary>
    public bool HasPendingUpdate { get; internal set; } = hasPendingUpdate;
}