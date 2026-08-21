using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Buffering.DoubleBuffering;

/// <summary>
/// A type of buffer that minimizes locking times during front buffer updates.
/// The back buffer should be updated concurrently with a back buffer controller.
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class DoubleBuffer<T>
{
    private DoubleBufferFrame<T> _frame;
    private readonly IResourceLock _lockImpl;
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
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="lockImpl">Lock implementation to use</param>
    /// <param name="swapEffect">Swap effect to use</param>
    public DoubleBuffer(T initialFrontValue, T initialBackValue, IResourceLock lockImpl, DoubleBufferSwapEffect swapEffect)
    {
        _frame = new DoubleBufferFrame<T>(initialFrontValue, initialBackValue, 0);
        _lockImpl = lockImpl;
        _swapEffect = swapEffect;
    }
    
    /// <summary>
    /// Locks the front buffer and reads it.
    /// The lock should be immediately disposed of in the same statement if T is a struct and contains no references
    /// </summary>
    /// <param name="rsc">Ref variable to read the buffer to</param>
    /// <param name="info">Minimal information about the current front buffer object</param>
    /// <returns>ResourceLockHandle to be disposed of immediately after reading/writing the buffer. This should be done ASAP</returns>
    internal T ReadFrontBuffer(out int version)
    {
        using var scope = _lockImpl.Lock(ResourceAccessFlags.Read);
        version = _frame.Version;
        return _frame.Front;
    }

    /// <summary>
    /// Updates the back buffer by updating the resource.
    /// Should be called before swapping the buffers and on a dedicated back buffer thread
    /// to maximize throughput.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done.
    /// </summary>
    internal void UpdateBackBuffer(in T value)
    {
        using var scope = _lockImpl.Lock(ResourceAccessFlags.Write);
        _frame.Back = value;
        _frame.Version++;
    }

    /// <summary>
    /// Reads the back buffer and returns a reference to it.
    /// </summary>
    /// <returns>A reference to the back buffer</returns>
    /// <exception cref="NotSupportedException">When the front buffer has not ben initially set for a reference return</exception>
    internal T ReadBackBuffer()
    {
        using var scope = _lockImpl.Lock(ResourceAccessFlags.Read);
        return _frame.Back;
    }

    /// <summary>
    /// Swaps the buffers with functionality according to the configured swap effect (default is flip).
    /// Should be called after updating the back buffer.
    /// All reads immediately after every swap are on the correct resource in the front buffer.
    /// The back buffer IS NOT THREAD SAFE. No locking or synchronization is done.
    /// This maximizes throughput out of the box.
    /// </summary>
    /// <exception cref="NotSupportedException">Unknown/unsupported swap effect</exception>
    internal void SwapBuffers()
    {
        using var scope = _lockImpl.Lock(ResourceAccessFlags.Write);
        switch (_swapEffect)
        {
            case DoubleBufferSwapEffect.Copy:
                // Front becomes the back; back keeps its own reference.
                // Both slots point to the same buffer after the swap.
                _frame.Front = _frame.Back;
                break;

            case DoubleBufferSwapEffect.Flip:
                // Front and back exchange references.
                (_frame.Front, _frame.Back) = (_frame.Back, _frame.Front);
                break;

            default:
                throw new NotSupportedException($"Unsupported swap effect: {_swapEffect}");
        }
    }
}