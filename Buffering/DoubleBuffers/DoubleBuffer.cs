namespace Buffering.DoubleBuffers
{
    public abstract class DoubleBuffer<T>
    {
        protected T Back;
        protected T Front;
        
        protected abstract void DrawBackBuffer();

        protected abstract void PresentBackBuffer();
    }
}