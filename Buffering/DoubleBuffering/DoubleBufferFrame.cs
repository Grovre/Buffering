namespace Buffering.DoubleBuffering;

public class DoubleBufferFrame<T>(T front, T back, int version)
{
    public T Front { get; internal set; } = front;
    public T Back { get; internal set; } = back;
    public int Version { get; internal set; } = version;
}