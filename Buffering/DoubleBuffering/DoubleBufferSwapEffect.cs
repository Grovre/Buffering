namespace Buffering.DoubleBuffering;

public enum DoubleBufferSwapEffect
{
    /// <summary>
    /// Flips the references to the resources in a double buffer.
    /// Should be used in most cases.
    /// Back buffer will receive what was in the front buffer. This can either be discarded or used for the next
    /// resource update unless the front buffer was using the initial resource object from initialization.
    /// </summary>
    Flip,
    /// <summary>
    /// Copies the back buffer resource to the front buffer resource.
    /// Generally slower than flipping, but not by a substantial amount unless the resource is large.
    /// Flip is best-suited for most cases but copy is best-suited for when
    /// the back buffer needs to know what is in the front buffer.
    /// </summary>
    Copy,
}