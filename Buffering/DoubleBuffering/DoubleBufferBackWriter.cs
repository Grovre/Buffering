namespace Buffering.DoubleBuffering;

/// <summary>
/// Provides dedicated, single-writer control to update, inspect, and swap the back buffer of a <see cref="DoubleBuffer{T}"/>.
/// </summary>
/// <typeparam name="T">The type of data stored in the double buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DoubleBufferBackWriter{T}"/> is a lightweight handle designed for the producer
/// thread in a single-writer multiple-reader (SWMR) concurrency model. It enables the writer to prepare and stage new data in the back buffer
/// in complete isolation from concurrent readers accessing the front buffer.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This handle enforces single-writer semantics. Only one thread should invoke write operations (<see cref="UpdateBackBuffer"/>
/// and <see cref="SwapBuffers"/>) at any given time. Concurrent calls from multiple threads will corrupt internal state.
/// </para>
/// <para>
/// <b>Performance:</b> Obtain an instance via <see cref="DoubleBuffer{T}.BackWriter"/> and cache it locally (e.g., in a local variable or a worker loop field)
/// to eliminate property invocation overhead in high-frequency update loops.
/// </para>
/// <para>
/// <b>Potential Gotchas &amp; Best Practices:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Mutating Recycled Reference Types:</b> When recycling objects via <see cref="ReadBackBuffer"/> under <see cref="DoubleBufferSwapEffect.FlipRefOrValue"/>, ensure reader threads have completed their read operations before mutating the object. If consumers hold the reference across swap intervals, in-place mutations will cause data races.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Staging In-Place Mutations:</b> When modifying an object retrieved from <see cref="ReadBackBuffer"/> in-place, you must call <see cref="UpdateBackBuffer"/> with that object to set the internal pending update flag. Failing to do so causes <see cref="SwapBuffers"/> to treat the update as absent and perform a no-op.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Aliasing under Copy Semantics:</b> Under <see cref="DoubleBufferSwapEffect.CopyRefOrValue"/>, both buffers point to the exact same object reference after a swap. Mutating the back buffer instance directly mutates the active front buffer without thread isolation.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public class DoubleBufferBackWriter<T>
{
    private readonly DoubleBuffer<T> _doubleBuffer;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBufferBackWriter{T}"/> class bound to the specified <see cref="DoubleBuffer{T}"/>.
    /// </summary>
    /// <param name="doubleBuffer">The <see cref="DoubleBuffer{T}"/> instance to control.</param>
    public DoubleBufferBackWriter(DoubleBuffer<T> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Updates the back buffer with a new value or reference and marks it as pending a swap.
    /// </summary>
    /// <param name="value">The new value or reference to write to the back buffer.</param>
    /// <remarks>
    /// <para>
    /// Writing to the back buffer does not alter the front buffer or interfere with concurrent reader threads.
    /// The staged data remains private to the writer until <see cref="SwapBuffers"/> is explicitly called.
    /// </para>
    /// <para>
    /// Multiple updates can be made before performing a swap; each call replaces the previous back buffer value.
    /// </para>
    /// </remarks>
    public void UpdateBackBuffer(T value)
    {
        _doubleBuffer.UpdateBackBuffer(value);
    }

    /// <summary>
    /// Reads the current value or reference stored in the back buffer.
    /// </summary>
    /// <returns>The current value or reference held in the back buffer.</returns>
    /// <remarks>
    /// <para>
    /// Allows the writer thread to inspect the back buffer before updating or swapping.
    /// </para>
    /// <para>
    /// When using <see cref="DoubleBufferSwapEffect.FlipRefOrValue"/>, after a swap this method returns the previous front buffer resource,
    /// enabling zero-allocation ping-pong recycling and in-place reuse of objects or memory buffers.
    /// </para>
    /// <para>
    /// <b>Gotcha:</b> If modifying an object returned by this method in-place, you must still invoke <see cref="UpdateBackBuffer"/> to mark
    /// the buffer as updated; otherwise <see cref="SwapBuffers"/> will treat the buffer as unchanged and perform a no-op.
    /// Additionally, ensure reader threads are not concurrently accessing this instance before mutating its fields.
    /// </para>
    /// </remarks>
    public T ReadBackBuffer()
    {
        return _doubleBuffer.ReadBackBuffer();
    }

    /// <summary>
    /// Swaps the back buffer into the front buffer in a lock-free, non-blocking manner according to the configured <see cref="DoubleBufferSwapEffect"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a swap was performed because a pending update was staged;
    /// <see langword="false"/> if no new update has been written since the last swap (or initialization), resulting in a no-op.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when an unsupported <see cref="DoubleBufferSwapEffect"/> is encountered.</exception>
    /// <remarks>
    /// <para>
    /// Swapping publishes the staged back buffer content to the front buffer, making it visible to all concurrent readers without locks.
    /// A memory barrier is issued to ensure immediate visibility across all CPU cores.
    /// </para>
    /// <para>
    /// If no new update was written via <see cref="UpdateBackBuffer"/> since the previous swap, the call returns <see langword="false"/>
    /// and performs no operation, preventing stale back buffer state from unintentionally overwriting the active front buffer.
    /// </para>
    /// </remarks>
    public bool SwapBuffers()
    {
        return _doubleBuffer.SwapBuffers();
    }
}