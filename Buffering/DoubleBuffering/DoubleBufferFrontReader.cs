namespace Buffering.DoubleBuffering;

/// <summary>
/// Provides high-performance, lock-free, read-only access to the front buffer of a <see cref="DoubleBuffer{T}"/>.
/// </summary>
/// <typeparam name="T">The type of data stored in the double buffer.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DoubleBufferFrontReader{T}"/> is a lightweight handle designed for consumer
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
/// <para>
/// <b>Potential Gotchas for Reference Types:</b>
/// When <typeparamref name="T"/> is a reference type, <see cref="ReadFrontBuffer"/> returns a direct reference to the object on the heap.
/// Consumers should treat this object as read-only. If the producer reuses objects with <see cref="DoubleBufferSwapEffect.FlipRefOrValue"/>,
/// retaining this reference across swap cycles while the producer mutates the recycled instance will lead to concurrent data races.
/// Consumers that need to retain state across frames should snapshot required fields or use immutable objects.
/// </para>
/// </remarks>
public class DoubleBufferFrontReader<T>
{
    private readonly DoubleBuffer<T> _doubleBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBufferFrontReader{T}"/> class bound to the specified <see cref="DoubleBuffer{T}"/>.
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
    /// Mutating this object or holding the reference across multiple swap cycles while the producer mutates recycled instances
    /// can cause race conditions.
    /// </para>
    /// <para>
    /// When <typeparamref name="T"/> is a value type, this returns a copy of the value currently published to the front buffer.
    /// For multi-word structs (larger than 64-bit on x64 platforms), non-atomic copies may lead to torn reads if a swap occurs concurrently.
    /// </para>
    /// </remarks>
    public T ReadFrontBuffer()
    {
        return _doubleBuffer.ReadFrontBuffer();
    }
}