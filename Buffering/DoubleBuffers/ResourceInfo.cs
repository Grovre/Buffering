namespace Buffering.DoubleBuffers;

public readonly record struct ResourceInfo
{
    public byte Id { get; }
    public byte FromBuffer { get; } = 0;

    public ResourceInfo()
    {
        Id = 0;
    }

    public ResourceInfo(byte id)
    {
        Id = id;
    }

    public ResourceInfo(byte id, byte fromBuffer)
    {
        Id = id;
        FromBuffer = fromBuffer;
    }
}