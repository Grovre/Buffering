namespace Buffering.DoubleBuffers;

/// <summary>
/// Represents metadata of the current front buffer resource.
/// Double buffer instances are updated every time the front buffer is updated.
/// </summary>
public readonly record struct ResourceInfo
{
    /// <summary>
    /// The 'u' in 'uint' stands for unique until overflow and Id rolls back to 0
    /// </summary>
    public uint Id { get; } = 0;
    /// <summary>
    /// Determines whether or not the current front buffer resource originates from the buffer.
    /// If the buffer has not updated the back buffer and swapped, this will be false
    /// </summary>
    public bool FromBuffer { get; } = false;

    /// <summary>
    /// Initializes using the given ID.
    /// FromBuffer will be initialized to false.
    /// </summary>
    /// <param name="id">The ID to use</param>
    public ResourceInfo(uint id)
    {
        Id = id;
        FromBuffer = false;
    }

    internal ResourceInfo(uint id, bool fromBuffer)
    {
        Id = id;
        FromBuffer = fromBuffer;
    }
}