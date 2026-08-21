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
/// Note that for structs larger than the native pointer/register size (e.g. multi-word structs), reading concurrently with a swap may lead to torn reads unless externally synchronized.
/// </para>
/// <para>
/// <b>Reference Types (<see langword="class"/>):</b> The buffer stores object references (pointers). Swapping swaps or copies reference pointers, enabling zero-allocation ping-pong recycling or reference sharing.
/// <para>
/// <b>Caution:</b> The double buffer synchronizes the pointer reference, not the internal fields of the object. Mutating an object instance while concurrent readers hold a reference to it will cause race conditions.
/// </para>
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
/// <b>Decoupled Handles:</b>
/// Access to the double buffer is mediated by lightweight handles:
/// <see cref="FrontReader"/> provides read-only access to the front buffer, while <see cref="BackWriter"/> provides update and swap capabilities for the back buffer.
/// Caching these instances locally avoids property dispatch overhead in tight performance loops.
/// </para>
/// <para>
/// <b>Pending Update Tracking:</b>
/// An internal flag tracks whether new data has been written to the back buffer. Calling <see cref="DoubleBufferBackWriter{T}.SwapBuffers"/> when no update has been staged since the last swap is a safe no-op that returns <see langword="false"/>, preventing active front buffer state from being unintentionally overwritten.
/// </para>
/// <para>
/// <b>Potential Gotchas &amp; Best Practices:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Mutating Shared Reference Types:</b> When <typeparamref name="T"/> is a mutable reference type, calling <see cref="DoubleBufferBackWriter{T}.ReadBackBuffer"/> and mutating fields in-place while reader threads still hold a reference from a previous <see cref="DoubleBufferFrontReader{T}.ReadFrontBuffer"/> call introduces concurrent data races. Ensure readers complete reading within their frame cycle, snapshot required fields, or use immutable data types.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Aliasing in <see cref="DoubleBufferSwapEffect.CopyRefOrValue"/>:</b> After a swap with copy semantics, both front and back slots point to the exact same object reference. Mutating the back buffer instance directly without assigning a new object will mutate the active front buffer without thread isolation.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>In-Place Mutation &amp; Pending Update Flag:</b> When mutating an existing back buffer object in-place, you must still invoke <see cref="DoubleBufferBackWriter{T}.UpdateBackBuffer"/> to mark the buffer as pending an update; otherwise, <see cref="DoubleBufferBackWriter{T}.SwapBuffers"/> will treat the buffer as unchanged and perform a no-op.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Multi-Word Struct Tearing:</b> For value types that exceed the CPU's atomic write width (typically 64 bits), concurrent reads and swaps may result in torn reads if not externally synchronized or wrapped in a reference type.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Single-Writer Violation:</b> Invoking write or swap methods on <see cref="BackWriter"/> from multiple threads concurrently without external locks will corrupt internal state.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public class DoubleBuffer<T>
{
    private T _front;
    private T _back;
    private bool _hasPendingUpdate;
    private readonly DoubleBufferSwapEffect _swapEffect;

    /// <summary>
    /// Gets a lightweight <see cref="DoubleBufferFrontReader{T}"/> handle for reading the front buffer.
    /// </summary>
    /// <remarks>
    /// Obtain this handle and cache it locally in consumer worker loops to eliminate property access overhead in high-frequency read scenarios.
    /// </remarks>
    public DoubleBufferFrontReader<T> FrontReader => new(this);

    /// <summary>
    /// Gets a lightweight <see cref="DoubleBufferBackWriter{T}"/> handle for updating, inspecting, and swapping the back buffer.
    /// </summary>
    /// <remarks>
    /// Obtain this handle and cache it locally in the producer worker loop to eliminate property access overhead in high-frequency write scenarios.
    /// </remarks>
    public DoubleBufferBackWriter<T> BackWriter => new(this);

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBuffer{T}"/> class with specified initial buffer values and a swap effect.
    /// </summary>
    /// <param name="initialFrontValue">The initial value or object reference placed in the front buffer, immediately readable by consumers.</param>
    /// <param name="initialBackValue">The initial value or object reference placed in the back buffer, available to the producer.</param>
    /// <param name="swapEffect">The <see cref="DoubleBufferSwapEffect"/> that determines how front and back buffers transition during a swap.</param>
    /// <remarks>
    /// The buffer is initialized with its pending update flag set to <see langword="true"/>, permitting an immediate initial swap of the provided values before any call to <see cref="DoubleBufferBackWriter{T}.UpdateBackBuffer"/>.
    /// </remarks>
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
    /// pending update to the process. Returns <c>false</c> if no swap occurred. Any
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