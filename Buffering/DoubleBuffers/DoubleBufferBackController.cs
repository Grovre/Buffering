namespace Buffering.DoubleBuffers;

public readonly struct DoubleBufferBackController<T>
    where T : struct
{
    public DoubleBuffer<T> DoubleBuffer { get; }
    
    public DoubleBufferBackController(DoubleBuffer<T> doubleBuffer)
    {
        DoubleBuffer = doubleBuffer;
    }

    public DoubleBufferBackController()
    {
        throw new NotImplementedException(
            "Back controller must be retrieved through a double buffer.");
    }

    public void UpdateBackBuffer()
    {
        DoubleBuffer.UpdateBackBuffer();
    }

    public void SwapBuffers()
    {
        DoubleBuffer.SwapBuffers();
    }
}