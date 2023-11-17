namespace Buffering.DoubleBuffers;

public enum DoubleBufferSwapEffect
{
    /// <summary>
    /// Flips the references to the resources in a double buffer.
    /// Should be used in most cases.
    /// Back buffer will receive what was in the front and can either be discarded or used for the next resource update unless the front buffer was using the initial resource object from initialization
    /// </summary>
    Flip,
    /// <summary>
    /// Copies the back buffer resource object to the front buffer resource object.
    /// Generally barely slower than flipping and not needed in most cases
    /// </summary>
    Copy,
}