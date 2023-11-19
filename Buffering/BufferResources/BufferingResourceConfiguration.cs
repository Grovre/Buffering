using Buffering.Locking;

namespace Buffering;

public class BufferingResourceConfiguration<T> : BufferConfiguration
    where T : struct
{
    public BufferingResource<T>.Initializer Init { get; set; }
    public BufferingResource<T>.ResourceUpdater Updater { get; set; }
    public IResourceLock ResourceLock { get; set; }

    public BufferingResourceConfiguration(BufferingResource<T>.Initializer init, BufferingResource<T>.ResourceUpdater updater, IResourceLock resourceLock)
    {
        Init = init;
        Updater = updater;
        ResourceLock = resourceLock;
    }
}