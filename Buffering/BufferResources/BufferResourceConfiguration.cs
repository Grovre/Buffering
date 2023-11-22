using Buffering.Locking;

namespace Buffering.BufferResources;

/// <summary>
/// Configuration used to create a resource with.
/// </summary>
/// <typeparam name="T">Type of value this resource represents</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public class BufferResourceConfiguration<T, TUpdaterState> : BufferConfiguration
    where T : struct
{
    /// <summary>
    /// Used during creation of a resource. Kept for copying a resource when needed
    /// </summary>
    public BufferResource<T, TUpdaterState>.Initializer Init { get; set; }
    /// <summary>
    /// Used to update the value of a resource
    /// </summary>
    public BufferResource<T, TUpdaterState>.ResourceUpdater Updater { get; set; }
    /// <summary>
    /// Used for locking onto a resource
    /// </summary>
    public IResourceLock ResourceLock { get; set; }

    /// <summary>
    /// Used for constructing a configuration to be used in the constructor of a resource
    /// </summary>
    /// <param name="init">Used during creation of a resource. Kept for copying a resource when needed</param>
    /// <param name="updater">Used to update the value of a resource</param>
    /// <param name="resourceLock">Used for locking onto a resource</param>
    public BufferResourceConfiguration(BufferResource<T, TUpdaterState>.Initializer init, BufferResource<T, TUpdaterState>.ResourceUpdater updater, IResourceLock resourceLock)
    {
        Init = init;
        Updater = updater;
        ResourceLock = resourceLock;
    }
}