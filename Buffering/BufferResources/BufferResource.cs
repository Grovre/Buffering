using System.Runtime.CompilerServices;
using Buffering.Locking;

namespace Buffering.BufferResources;

/// <summary>
/// An object representing a resource object in a buffer
/// </summary>
/// <typeparam name="T">Value type of resource object</typeparam>
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
    public T Resource => _resource;

    /// <summary>
    /// State passed into the updater to avoid capturing
    /// </summary>

    private readonly BufferResourceConfiguration<T, TUpdaterState> _config;

    /// <summary>
    /// Normal constructor. Resource is initialized to what init returns and IsResourceFromUpdater is initialized to false.
    /// </summary>
    /// <param name="init">Initial value for the resource</param>
    /// <param name="updater">Resource object updater</param>
    public BufferResource(BufferResourceConfiguration<T, TUpdaterState> configuration)
    {
        _config = new BufferResourceConfiguration<T, TUpdaterState>(
            configuration.Init,
            configuration.Updater,
            configuration.ResourceLock.Copy());
    }

    /// <summary>
    /// Copy constructor.
    /// The resource's object is initialized to what init will return,
    /// not be assigned to the other's resource object
    /// </summary>
    /// <param name="other">Resource to copy from</param>
    public BufferResource(BufferResource<T, TUpdaterState> other, bool skipInit = false)
    {
        _config = other._config;
        if (!skipInit)
            _config.Init(out _resource);
    }
    
    #region LockingMechanisms

    public ResourceLockHandle Lock(ResourceAccessFlag flags)
    {
        return _config.ResourceLock.Lock(flags);
    }

    public bool TryLock(ResourceAccessFlag flags, out ResourceLockHandle hlock)
    {
        return _config.ResourceLock.TryLock(flags, out hlock);
    }

    #endregion

    /// <summary>
    /// Uses the updater to update the resource object.
    /// IsResourceFromUpdater becomes true.
    /// </summary>
    public void UpdateResource(TUpdaterState state)
    {
        _config.Updater(ref _resource, IsResourceFromUpdater, state);
        IsResourceFromUpdater = true;
    }

    internal void CopyFromResource(BufferResource<T, TUpdaterState> other)
    {
        _resource = other._resource;
    }
}