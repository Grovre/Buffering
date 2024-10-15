namespace Buffering.DoubleBuffering;

/// <summary>
/// Different types of swap effects for double buffering
/// </summary>
public enum DoubleBufferSwapEffect
{
    /// <summary>
    /// Flips the references to the resources in a double buffer.
    /// Should be used in most cases.
    /// Back buffer will receive what was in the front buffer. This can either be discarded or used for the next
    /// resource update unless the front buffer was using the initial resource object from initialization.
    /// </summary>
    Flip
}