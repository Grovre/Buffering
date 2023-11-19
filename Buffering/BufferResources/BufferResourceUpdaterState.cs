namespace Buffering.BufferResources;

public struct BufferResourceUpdaterState<T>
{
    public T State { get; set; }

    public BufferResourceUpdaterState(T initialState)
    {
        State = initialState;
    }
}