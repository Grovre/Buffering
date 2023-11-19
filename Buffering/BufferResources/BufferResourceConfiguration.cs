using Buffering.Locking;

namespace Buffering.BufferResources;

public class BufferResourceConfiguration<T, TUpdaterState> : BufferConfiguration
    where T : struct
{
    public BufferResource<T, TUpdaterState>.Initializer Init { get; set; }
    public BufferResource<T, TUpdaterState>.ResourceUpdater Updater { get; set; }
    public IResourceLock ResourceLock { get; set; }

    public BufferResourceConfiguration(BufferResource<T, TUpdaterState>.Initializer init, BufferResource<T, TUpdaterState>.ResourceUpdater updater, IResourceLock resourceLock)
    {
        Init = init;
        Updater = updater;
        ResourceLock = resourceLock;
    }
}