using System.Runtime.CompilerServices;

namespace Buffering;

/// <summary>
/// An object representing a resource object in a buffer
/// </summary>
/// <typeparam name="T">Value type of resource object</typeparam>
public class BufferingResource<T>
    where T : struct
{
    /// <summary>
    /// The functionality for updating the resource.
    /// Includes a boolean for determining whether the resource was updated by a previous call from the resource updater or not
    /// </summary>
    public delegate void ResourceUpdater(ref T rsc, bool fromUpdater);

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
    private readonly ResourceUpdater _updater;
    private readonly Func<T> _init;

    /// <summary>
    /// Normal constructor. Resource is initialized to what init returns and IsResourceFromUpdater is initialized to false.
    /// </summary>
    /// <param name="init">Initial value for the resource</param>
    /// <param name="updater">Resource object updater</param>
    public BufferingResource(Func<T> init, ResourceUpdater updater)
    {
        IsResourceFromUpdater = false;
        _init = init;
        _resource = init();
        _updater = updater;
    }

    /// <summary>
    /// Copy constructor.
    /// The resource's object is initialized to what init will return,
    /// not be assigned to the other's resource object
    /// </summary>
    /// <param name="other">Resource to copy from</param>
    public BufferingResource(BufferingResource<T> other)
    {
        IsResourceFromUpdater = false;
        _init = other._init;
        _resource = _init();
        _updater = other._updater;
    }

    /// <summary>
    /// Uses the updater to update the resource object.
    /// IsResourceFromUpdater becomes true.
    /// </summary>
    public void UpdateResource()
    {
        _updater(ref _resource, IsResourceFromUpdater);
        IsResourceFromUpdater = true;
    }

    internal void SetDefault()
    {
        IsResourceFromUpdater = false;
        _resource = _init();
    }

    internal void CopyFromResource(BufferingResource<T> other)
    {
        _resource = other._resource;
    }
}