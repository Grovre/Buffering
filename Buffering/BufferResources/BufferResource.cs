using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Buffering.BufferResources;

/// <summary>
/// An object representing a resource object in a buffer
/// </summary>
/// <typeparam name="T">Value type of resource object</typeparam>
/// <typeparam name="TUpdaterState">Type of object used for state in the updater delegate</typeparam>
public class BufferResource<T, TUpdaterState>
    where T : struct
{
    /// <summary>
    /// The functionality for updating the resource.
    /// Includes a boolean for determining whether the resource was updated by a previous call from the resource updater or not.
    /// Never lock on objects outside of the function as this function may be shared between more resources.
    /// </summary>
    public delegate void ResourceUpdater(ref T rsc, bool fromUpdater, TUpdaterState state);
    
    /// <summary>
    /// Used for initializing the resource object. This is what will everything see before the first update.
    /// </summary>
    public delegate void Initializer(out T rsc);

    /// <summary>
    /// If the resource type is or contains reference type member variables, this is true
    /// </summary>
    public static readonly bool ResourceIsOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    private T _resource;
    /// <summary>
    /// Whether or not the resource object has been through the updater
    /// </summary>
    public bool IsResourceFromUpdater { get; private set; }

    /// <summary>
    /// The object represented by this resource
    /// </summary>
    public T Resource
    {
        get => _resource;
        set => _resource = value;
    }

    /// <summary>
    /// State passed into the updater to avoid capturing
    /// </summary>

    internal readonly BufferResourceConfiguration<T, TUpdaterState> Config;

    /// <summary>
    /// Normal constructor. Resource is initialized to what init returns and IsResourceFromUpdater is initialized to false.
    /// </summary>
    /// <param name="configuration">How the resource should be configured</param>
    public BufferResource(BufferResourceConfiguration<T, TUpdaterState> configuration)
    {
        Config = new BufferResourceConfiguration<T, TUpdaterState>(
            configuration.Init,
            configuration.Updater,
            configuration.ResourceLock.Copy());
        
        Config.Init(out _resource);
    }

    /// <summary>
    /// Copy constructor.
    /// The resource's object is initialized to what init will return,
    /// not be assigned to the other's resource object
    /// </summary>
    /// <param name="other">Resource to copy from</param>
    /// <param name="skipInit">If enabled, resource is not initialized with Init delegate</param>
    public BufferResource(BufferResource<T, TUpdaterState> other, bool skipInit = false)
    {
        Config = other.Config;
        if (!skipInit)
            Config.Init(out _resource);
    }
    
    #region LockingMechanisms

    /// <summary>
    /// Uses the IResourceLock to lock onto this resource.
    /// </summary>
    /// <param name="flags">The intent of locking onto the resource. Useful when configured with MultipleReaderLock</param>
    /// <returns>A disposable value used to release the lock.</returns>
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        return Config.ResourceLock.Lock(flags);
    }

    /// <summary>
    /// Attempts to use the IResourceLock to lock onto this resource.
    /// </summary>
    /// <param name="flags">The intent of locking onto the resource. Useful when configured with MultipleReaderLock.
    /// If MultipleReaderLock is used, this will only return false when resource is being written to.</param>
    /// <param name="hlock">A disposable value used to release the lock.</param>
    /// <returns>Whether or not the resource was locked onto</returns>
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        return Config.ResourceLock.TryLock(flags, out hlock);
    }

    #endregion

    /// <summary>
    /// Uses the updater to update the resource object.
    /// IsResourceFromUpdater becomes true.
    /// </summary>
    public void UpdateResource(TUpdaterState state)
    {
        Config.Updater(ref _resource, IsResourceFromUpdater, state);
        IsResourceFromUpdater = true;
    }

    internal void CopyFromResource(BufferResource<T, TUpdaterState> other)
    {
        _resource = other._resource;
    }
}