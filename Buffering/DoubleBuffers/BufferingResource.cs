namespace Buffering.DoubleBuffers;

public struct BufferingResource<T>
    where T : struct
{
    public delegate void ResourceUpdater(out T rsc);

    private T _resource;
    public T Resource { get; }
    private readonly ResourceUpdater _updater;

    public BufferingResource(ResourceUpdater updater)
    {
        _updater = updater;
    }

    public BufferingResource()
    {
        _updater = (out T rsc) => rsc = default;
    }

    internal void UpdateResource()
    {
        _updater(out _resource);
    }
}