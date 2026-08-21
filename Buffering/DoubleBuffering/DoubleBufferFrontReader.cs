namespace Buffering.DoubleBuffering;

/// <summary>
/// Provides high-performance, lock-free, read-only access to the front buffer of a <see cref="DoubleBuffer{T}"/>.
/// </summary>
/// <typeparam name="T">The type of data stored in the double buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DoubleBufferFrontReader{T}"/> is a lightweight <see langword="readonly"/> <see langword="struct"/> handle designed for consumer
/// threads in a single-writer multiple-reader (SWMR) concurrency model. Multiple reader threads can safely access and read the front
/// buffer concurrently without locking, blocking, or contending with the writer thread.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Completely safe for concurrent invocation by multiple reader threads.
/// </para>
/// <para>
/// <b>Performance:</b> Obtain an instance via <see cref="DoubleBuffer{T}.FrontReader"/> and cache it locally (e.g., in a local variable or a render/processing loop)
/// to eliminate property invocation overhead in high-frequency read loops.
/// </para>
/// </remarks>
public class DoubleBufferFrontReader<T>
{
    private readonly DoubleBuffer<T> _doubleBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBufferFrontReader{T}"/> struct bound to the specified <see cref="DoubleBuffer{T}"/>.
    /// </summary>
    /// <param name="doubleBuffer">The <see cref="DoubleBuffer{T}"/> instance to read from.</param>
    public DoubleBufferFrontReader(DoubleBuffer<T> doubleBuffer)
    {
        _doubleBuffer = doubleBuffer;
    }

    /// <summary>
    /// Reads the current value or reference from the front buffer without acquiring locks.
    /// </summary>
    /// <returns>The current value or reference held in the front buffer.</returns>
    /// <remarks>
    /// <para>
    /// Safe for multiple concurrent readers. Reading from the front buffer never blocks other readers or the writer thread.
    /// A memory barrier ensures that the latest swapped data is immediately visible across all CPU caches.
    /// </para>
    /// <para>
    /// When <typeparamref name="T"/> is a reference type, this returns the reference currently published to the front buffer.
    /// When <typeparamref name="T"/> is a value type, this returns a copy of the value currently published to the front buffer.
    /// </para>
    /// </remarks>
    public T ReadFrontBuffer()
    {
        return _doubleBuffer.ReadFrontBuffer();
    }
}