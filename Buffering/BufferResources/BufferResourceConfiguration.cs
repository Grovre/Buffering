using Buffering.Locking;

namespace Buffering.BufferResources;

public class BufferResourceConfiguration<T> : BufferConfiguration
    where T : struct
{
    public BufferResource<T>.Initializer Init { get; set; }
    public BufferResource<T>.ResourceUpdater Updater { get; set; }
    public IResourceLock ResourceLock { get; set; }

    public BufferResourceConfiguration(BufferResource<T>.Initializer init, BufferResource<T>.ResourceUpdater updater, IResourceLock resourceLock)
    {
        Init = init;
        Updater = updater;
        ResourceLock = resourceLock;
    }
}