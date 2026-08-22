using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Benchmarking.OldDoubleBuffering;

/// <summary>
/// A type of buffer that minimizes locking times during front buffer updates (commit 4a9c72b4).
/// </summary>
/// <typeparam name="T">Value type in the buffer</typeparam>
public class OldDoubleBuffer<T>
{
    private StrongBox<T> _rsc0; // front
    private bool _frontUpdated = false;
    private StrongBox<T> _rsc1; // back
    private bool _backUpdated = false;
    private BufferedResourceInfo _frontInfo;
    private readonly IResourceLock _lock;
    private readonly OldDoubleBufferSwapEffect _swapEffect;

    /// <summary>
    /// Used to create a local value to read the front buffer from.
    /// </summary>
    public OldDoubleBufferFrontReader<T> FrontReader => new(this);

    /// <summary>
    /// Used to create a local value to update and swap the back buffer.
    /// </summary>
    public OldDoubleBufferBackWriter<T> BackWriter => new(this);

    /// <summary>
    /// Constructs the double buffer accordingly.
    /// </summary>
    /// <param name="lockImpl">Lock implementation to use</param>
    /// <param name="swapEffect">Swap effect to use</param>
    public OldDoubleBuffer(IResourceLock lockImpl, OldDoubleBufferSwapEffect swapEffect = OldDoubleBufferSwapEffect.Flip)
    {
        _rsc0 = new();
        _rsc1 = new();
        _frontInfo = default;
        _lock = lockImpl.Copy();
        _swapEffect = swapEffect;
    }

    /// <summary>
    /// Locks the front buffer and reads it.
    /// </summary>
    public ResourceLockHandle ReadFrontBuffer(out T rsc, out BufferedResourceInfo info)
    {
        var hlock = _lock.Lock(ResourceAccessFlags.Read);
        rsc = _rsc0.Value!;
        info = _frontInfo;
        return hlock;
    }

    /// <summary>
    /// Updates the back buffer by updating the resource.
    /// </summary>
    public void UpdateBackBuffer(in T value)
    {
        _rsc1.Value = value;
        _backUpdated = true;
    }

    /// <summary>
    /// Reads the back buffer and returns a reference to it.
    /// </summary>
    public ref T ReadBackBuffer()
    {
        if (!_frontUpdated || !_backUpdated)
            throw new NotSupportedException(
                "A buffer has not been initialized for a reference return");

        return ref _rsc1.Value!;
    }

    /// <summary>
    /// Swaps the buffers with functionality according to the configured swap effect.
    /// </summary>
    public void SwapBuffers()
    {
        var nextInfo = BufferedResourceInfo.PrepareNextInfo(_frontInfo, true);

        switch (_swapEffect)
        {
            case OldDoubleBufferSwapEffect.Flip:
                var t = _rsc0;
                var hlock1 = _lock.Lock(ResourceAccessFlags.Write);
                _rsc0 = _rsc1;
                _frontInfo = nextInfo;
                hlock1.Dispose(); // Quick release
                _rsc1 = t;

                (_backUpdated, _frontUpdated) = (_frontUpdated, _backUpdated);
                break;

            default:
                throw new NotSupportedException(
                    "Unsupported swap effect");
        }
    }
}
