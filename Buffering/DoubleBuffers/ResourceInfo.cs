namespace Buffering.DoubleBuffers;

public readonly record struct ResourceInfo
{
    public uint Id { get; }
    public bool FromBuffer { get; } = false;

    public ResourceInfo()
    {
        Id = 0;
    }

    public ResourceInfo(uint id)
    {
        Id = id;
    }

    public ResourceInfo(uint id, bool fromBuffer)
    {
        Id = id;
        FromBuffer = fromBuffer;
    }
}