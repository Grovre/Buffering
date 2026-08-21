using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A high-performance, lock-free, single-writer multiple-reader (SWMR) double buffer that enables concurrent reading and writing without locks or thread blocking.
/// </summary>
/// <typeparam name="T">
/// The type of data stored in the double buffer.
/// <para>
/// <b>Value Types (<see langword="struct"/>):</b> The buffer stores distinct copies of the struct value. Swapping moves or copies values directly without heap allocations.
/// </para>
/// <para>
/// <b>Reference Types (<see langword="class"/>):</b> The buffer stores object references (pointers). Swapping swaps or copies reference pointers, enabling zero-allocation ping-pong recycling or reference sharing.
/// </para>
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Single-Writer Multiple-Reader (SWMR) Model:</b>
/// <see cref="DoubleBuffer{T}"/> is optimized for scenarios where a single producer thread writes and publishes data, while one or more consumer threads concurrently read the latest published data.
/// Reading and writing occur on separate internal buffers, eliminating mutual exclusion and lock contention.
/// </para>
/// <para>
/// <b>Memory &amp; Thread Safety Guarantees:</b>
/// Synchronization is entirely lock-free, relying on memory barriers (<see cref="Interlocked.MemoryBarrier"/>) to guarantee cache coherency and immediate visibility across CPU cores without blocking threads.
/// Only a single writer thread should access <see cref="BackWriter"/> at any given time, whereas any number of reader threads can concurrently access <see cref="FrontReader"/>.
/// </para>
/// <para>
/// <b>Reference vs. Value Type Semantics:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Value Types:</b> Each buffer contains its own value copy. Mutations to local copies do not affect the internal buffers.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Reference Types:</b> The buffers hold references to objects on the heap. With <see cref="DoubleBufferSwapEffect.FlipRefOrValue"/>, the references are swapped, allowing the writer to reuse and mutate the old front buffer instance for the next frame with zero allocations. With <see cref="DoubleBufferSwapEffect.CopyRefOrValue"/>, both buffers will reference the same object instance after a swap until the back buffer reference is replaced; caution is advised when using mutable objects with copy semantics.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Decoupled Struct Handles:</b>
/// Access to the double buffer is mediated by lightweight <see langword="readonly"/> <see langword="struct"/> handles:
/// <see cref="FrontReader"/> provides read-only access to the front buffer, while <see cref="BackWriter"/> provides update and swap capabilities for the back buffer.
/// Caching these struct instances locally avoids property dispatch overhead in tight performance loops.
/// </para>
/// <para>
/// <b>Pending Update Tracking:</b>
/// An internal flag tracks whether new data has been written to the back buffer. Calling <see cref="DoubleBufferBackWriter{T}.SwapBuffers"/> when no update has been staged since the last swap is a safe no-op that returns <see langword="false"/>, preventing active front buffer state from being unintentionally overwritten.
/// </para>
/// </remarks>
public class DoubleBuffer<T>
{
    private T _front;
    private T _back;
    private bool _hasPendingUpdate;
    private readonly DoubleBufferSwapEffect _swapEffect;

    /// <summary>
    /// Provides access to the front buffer of a double buffering mechanism.
    /// </summary>
    public DoubleBufferFrontReader<T> FrontReader => new(this);

    /// <summary>
    /// Provides access to the back buffer of a double buffering mechanism.
    /// </summary>
    public DoubleBufferBackWriter<T> BackWriter => new(this);

    /// <summary>
    /// Represents a double-buffering mechanism, enabling seamless swapping
    /// between front and back buffers to handle updates without blocking or
    /// race conditions.
    /// </summary>
    /// <typeparam name="T">The type of the data stored within the buffers.</typeparam>
    public DoubleBuffer(T initialFrontValue, T initialBackValue, DoubleBufferSwapEffect swapEffect)
    {
        _front = initialFrontValue;
        _back = initialBackValue;
        _hasPendingUpdate = true;
        _swapEffect = swapEffect;
    }

    /// <summary>
    /// Provides access to the current front buffer, allowing read operations
    /// on the most recently available data.
    /// </summary>
    /// <returns>The current front buffer containing the latest data.</returns>
    internal T ReadFrontBuffer()
    {
        Interlocked.MemoryBarrier(); // Can't use Volatile.Read unless T is constrained to class or using hardcoded primitives
        return _front;
    }

    /// <summary>
    /// Updates the back buffer with the specified value and marks it for a pending update.
    /// </summary>
    /// <param name="value">The value to be stored in the back buffer.</param>
    internal void UpdateBackBuffer(T value)
    {
        _back = value;
        _hasPendingUpdate = true;
    }

    /// <summary>
    /// Retrieves the back buffer, which holds the data not currently being accessed for updates,
    /// allowing safe read-only access to the stored contents.
    /// </summary>
    /// <returns>The back buffer containing the stored data of type <typeparamref name="T"/>.</returns>
    internal T ReadBackBuffer()
    {
        return _back;
    }

    /// <summary>
    /// Swaps the front and back buffers based on the configured swap effect.
    /// This enables updating the active buffer while ensuring thread safety and
    /// consistency of the data being accessed.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if a swap was performed, indicating that there was a
    /// pending update to process. Returns <c>false</c> if no swap occurred. Any
    /// updates to the back buffer will flag a pending update.
    /// </returns>
    internal bool SwapBuffers()
    {
        if (!_hasPendingUpdate)
            return false;

        switch (_swapEffect)
        {
            case DoubleBufferSwapEffect.FlipRefOrValue:
                (_front, _back) = (_back, _front);
                break;
            case DoubleBufferSwapEffect.CopyRefOrValue:
                _front = _back;
                break;
            default:
                throw new NotSupportedException($"Unsupported swap effect: {_swapEffect}");
        }

        Interlocked.MemoryBarrier();
        _hasPendingUpdate = false;
        return true;
    }
}