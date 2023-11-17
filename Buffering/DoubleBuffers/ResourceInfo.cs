namespace Buffering.DoubleBuffers;

public readonly record struct ResourceInfo
{
    public uint Id { get; } = 0;
    public bool FromBuffer { get; } = false;

    public ResourceInfo(uint id)
    {
        Id = id;
        FromBuffer = false;
    }

    public ResourceInfo(uint id, bool fromBuffer)
    {
        Id = id;
        FromBuffer = fromBuffer;
    }
}