using System.Runtime.CompilerServices;

namespace Buffering.DoubleBuffers;

public class BufferingResource<T>
    where T : struct
{
    public delegate void ResourceUpdater(ref T rsc);

    public static readonly bool ResourceIsOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    private T _resource;
    public bool IsResourceFromUpdater { get; private set; }
    public T Resource => _resource;
    private readonly ResourceUpdater _updater;
    private readonly Func<T> _init;

    public BufferingResource(Func<T> init, ResourceUpdater updater)
    {
        IsResourceFromUpdater = false;
        _init = init;
        _resource = init();
        _updater = updater;
    }

    public BufferingResource(BufferingResource<T> other)
    {
        IsResourceFromUpdater = false;
        _init = other._init;
        _resource = _init();
        _updater = other._updater;
    }

    internal void UpdateResource()
    {
        _updater(ref _resource);
        IsResourceFromUpdater = true;
    }

    internal void SetDefault()
    {
        IsResourceFromUpdater = false;
        _resource = _init();
    }

    // NOTE: Structs implementing ICloneable with member reference variables WILL BE BOXED
    internal void CopyFromResource(BufferingResource<T> other)
    {
        if (ResourceIsOrContainsReferences)
        {
            if (other._resource is not ICloneable cloneableRsc)
                throw new Exception("Cannot clone type that is a reference or contains references without ICloneable");

            _resource = (T)cloneableRsc.Clone();
        }
        
        _resource = other._resource;
    }
}