namespace Buffering.DoubleBuffers;

public struct DoubleBufferBackController<T>
    where T : struct
{
    public DoubleBuffer<T> DoubleBuffer { get; private set; }

    public void UpdateBackBuffer()
    {
        DoubleBuffer.UpdateBackBuffer();
    }

    public void SwapBuffers()
    {
        DoubleBuffer.SwapBuffers();
    }

    public DoubleBufferBackController(DoubleBuffer<T> doubleBuffer)
    {
        DoubleBuffer = doubleBuffer;
    }

    public DoubleBufferBackController()
    {
        throw new NotImplementedException(
            "Back controller must be retrieved through a double buffer.");
    }
}