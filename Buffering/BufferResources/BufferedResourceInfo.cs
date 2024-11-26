namespace Buffering.BufferResources;

/// <summary>
/// Represents metadata of the current front buffer resource.
/// Double buffer instances are updated every time the front buffer is updated.
/// </summary>
public readonly record struct BufferedResourceInfo
{
    /// <summary>
    /// General way to keep track of equality. Id is not completely unique, as it will wrap around back to 0.
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
    public BufferedResourceInfo(uint id)
    {
        Id = id;
        FromBuffer = false;
    }

    internal BufferedResourceInfo(uint id, bool fromBuffer)
    {
        Id = id;
        FromBuffer = fromBuffer;
    }

    internal static BufferedResourceInfo PrepareNextInfo(in BufferedResourceInfo currentInfo, bool fromBuffer)
    {
        var id = unchecked(currentInfo.Id + 1);
        return new BufferedResourceInfo(id, fromBuffer);
    }
}