namespace Buffering.DoubleBuffering;

/// <summary>
/// Defines the transition behavior applied to the front and back buffers during a swap operation in <see cref="DoubleBuffer{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Double buffering isolates reader and writer operations by maintaining two internal buffer slots.
/// The choice of <see cref="DoubleBufferSwapEffect"/> determines whether the buffer slots are exchanged (ping-ponged)
/// or whether the back buffer content is propagated forward while retaining its state in the back buffer.
/// </para>
/// <para>
/// <b>Value Types vs. Reference Types:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <b><see cref="FlipRefOrValue"/>:</b> For value types, the two struct values are exchanged. For reference types, the object references (pointers) are exchanged, allowing zero-allocation recycling of allocated buffers or objects.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b><see cref="CopyRefOrValue"/>:</b> For value types, the struct value in the back buffer is copied into the front buffer. For reference types, the reference pointer in the back buffer is copied into the front buffer, meaning both slots will point to the same instance until a new reference is assigned to the back buffer.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Important Concurrency Gotchas:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       When <c>T</c> is a mutable reference type with <see cref="FlipRefOrValue"/>, ensure readers do not hold references across swap intervals while the writer is mutating the recycled instance in the back buffer.
///     </description>
///   </item>
///   <item>
///     <description>
///       When <c>T</c> is a mutable reference type with <see cref="CopyRefOrValue"/>, modifying the back buffer instance directly mutates the active front buffer without thread isolation because both buffer slots alias the same object.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public enum DoubleBufferSwapEffect
{
    /// <summary>
    /// Exchanges (flips) the front and back buffers (<c>(_front, _back) = (_back, _front)</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the recommended swap effect for most high-performance scenarios.
    /// Upon swapping, the back buffer receives the previous front buffer value or reference.
    /// </para>
    /// <para>
    /// <b>Zero-Allocation Recycling:</b> When <c>T</c> is a reference type (such as an array, custom buffer class, or frame object),
    /// the writer can inspect and reuse the instance now resting in the back buffer (<see cref="DoubleBufferBackWriter{T}.ReadBackBuffer"/>)
    /// to prepare the next frame, eliminating heap allocations and GC pressure entirely.
    /// </para>
    /// <para>
    /// <b>Reference Type Gotchas:</b> If the recycled object is mutated in-place by the writer, ensure that reader threads are not still reading from that same instance from a previous frame. Readers should finish processing or clone the required data before the writer mutates it in the back buffer. Furthermore, when mutating the recycled instance in-place, the writer must still call <see cref="DoubleBufferBackWriter{T}.UpdateBackBuffer"/> to mark the buffer as pending an update before calling <see cref="DoubleBufferBackWriter{T}.SwapBuffers"/>.
    /// </para>
    /// <para>
    /// <b>Value Types:</b> When <c>T</c> is a value type, the struct values in the front and back slots are swapped in-place.
    /// </para>
    /// </remarks>
    FlipRefOrValue,

    /// <summary>
    /// Copies the back buffer to the front buffer (<c>_front = _back</c>) while preserving the back buffer's current state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Upon swapping, the front buffer is overwritten with the current back buffer content, while the back buffer remains unchanged.
    /// </para>
    /// <para>
    /// <b>Persistent State &amp; Delta Updates:</b> Useful when the writer needs to maintain a continuous baseline across multiple updates
    /// (e.g., incremental state accumulation, delta compression, or persistent game/simulation state) without needing to re-copy or re-synchronize state from the previous front buffer.
    /// </para>
    /// <para>
    /// <b>Reference Type Considerations &amp; Gotchas:</b> When <c>T</c> is a reference type, both front and back buffers will reference the exact same object
    /// instance in memory following the swap until <see cref="DoubleBufferBackWriter{T}.UpdateBackBuffer"/> assigns a new reference.
    /// If <c>T</c> is mutable, modifying that instance directly while it is published in the front buffer modifies the front buffer without thread isolation, leading to data races.
    /// To avoid this, use immutable data structures or assign new distinct object instances when modifying state with copy semantics.
    /// </para>
    /// </remarks>
    CopyRefOrValue
}