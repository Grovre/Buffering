using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A lock-free double buffer that enables concurrent reading and writing without blocking or locks.
/// The back buffer can be updated and swapped concurrently while readers access the front buffer.
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class DoubleBuffer<T>
{
    private DoubleBufferFrame<T> _frame;
    private readonly DoubleBufferSwapEffect _swapEffect;

    /// <summary>
    /// Used to create a local value to read the front buffer from.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferFrontReader<T> FrontReader => new(this);
    /// <summary>
    /// Used to create a local value to update and swap the back buffer.
    /// Using this locally can provide great performance benefits.
    /// </summary>
    public DoubleBufferBackWriter<T> BackWriter => new(this);

    /// <summary>
    /// Constructs a lock-free double buffer with the specified initial values and swap effect.
    /// </summary>
    /// <param name="initialFrontValue">Initial value for the front buffer</param>
    /// <param name="initialBackValue">Initial value for the back buffer</param>
    /// <param name="swapEffect">Swap effect to use (default is Flip)</param>
    public DoubleBuffer(T initialFrontValue, T initialBackValue, DoubleBufferSwapEffect swapEffect = DoubleBufferSwapEffect.Flip)
    {
        _frame = new DoubleBufferFrame<T>(initialFrontValue, initialBackValue, hasPendingUpdate: true);
        _swapEffect = swapEffect;
    }

    /// <summary>
    /// Constructs a lock-free double buffer.
    /// </summary>
    /// <param name="initialFrontValue">Initial value for the front buffer</param>
    /// <param name="initialBackValue">Initial value for the back buffer</param>
    /// <param name="lockImpl">Ignored. DoubleBuffer is entirely lock-free.</param>
    /// <param name="swapEffect">Swap effect to use</param>
    [Obsolete("DoubleBuffer is entirely lock-free; lockImpl is ignored.")]
    public DoubleBuffer(T initialFrontValue, T initialBackValue, IResourceLock? lockImpl, DoubleBufferSwapEffect swapEffect = DoubleBufferSwapEffect.Flip)
        : this(initialFrontValue, initialBackValue, swapEffect)
    {
    }
    
    /// <summary>
    /// Reads the front buffer without locking.
    /// </summary>
    /// <returns>The front buffer value</returns>
    internal T ReadFrontBuffer()
    {
        var frame = Volatile.Read(ref _frame);
        return frame.Front;
    }

    /// <summary>
    /// Updates the back buffer in a lock-free manner and marks it as pending swap.
    /// Does not modify the front buffer.
    /// Should be called before swapping the buffers.
    /// </summary>
    /// <param name="value">The new value for the back buffer</param>
    internal void UpdateBackBuffer(in T value)
    {
        DoubleBufferFrame<T> current;
        DoubleBufferFrame<T> next;
        do
        {
            current = Volatile.Read(ref _frame);
            next = new DoubleBufferFrame<T>(current.Front, value, hasPendingUpdate: true);
        } while (Interlocked.CompareExchange(ref _frame, next, current) != current);
    }

    /// <summary>
    /// Reads the back buffer in a lock-free manner.
    /// </summary>
    /// <returns>The current back buffer value</returns>
    internal T ReadBackBuffer()
    {
        var frame = Volatile.Read(ref _frame);
        return frame.Back;
    }

    /// <summary>
    /// Swaps the buffers in a lock-free, last-writer-wins manner according to the configured swap effect.
    /// If no new update has been written to the back buffer since the last swap, the swap is a no-op to prevent overwriting newer front buffer data.
    /// Should be called after updating the back buffer to publish the new frame to readers.
    /// </summary>
    /// <exception cref="NotSupportedException">When an unknown/unsupported swap effect is encountered</exception>
    internal void SwapBuffers()
    {
        DoubleBufferFrame<T> current;
        DoubleBufferFrame<T> next;
        do
        {
            current = Volatile.Read(ref _frame);
            if (!current.HasPendingUpdate)
            {
                return;
            }

            var (newFront, newBack) = _swapEffect switch
            {
                DoubleBufferSwapEffect.Copy => (current.Back, current.Back),
                DoubleBufferSwapEffect.Flip => (current.Back, current.Front),
                _ => throw new NotSupportedException($"Unsupported swap effect: {_swapEffect}")
            };
            next = new DoubleBufferFrame<T>(newFront, newBack, hasPendingUpdate: false);
        } while (Interlocked.CompareExchange(ref _frame, next, current) != current);
    }
}