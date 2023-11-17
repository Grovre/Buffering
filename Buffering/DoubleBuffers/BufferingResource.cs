namespace Buffering.DoubleBuffers;

public struct BufferingResource<T>
{
    public delegate void ResourceUpdater(ref T rsc);

    private T _resource;
    public T Resource => _resource;
    private readonly ResourceUpdater _updater;

    public BufferingResource(T init, ResourceUpdater updater)
    {
        _resource = init;
        _updater = updater;
    }

    internal void UpdateResource()
    {
        _updater(ref _resource);
    }
}