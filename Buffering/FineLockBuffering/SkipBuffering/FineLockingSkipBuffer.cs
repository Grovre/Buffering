using Buffering.BufferResources;
using Buffering.Locking;

namespace Buffering.FineLockBuffering.SkipBuffering;

public class FineLockingSkipBuffer<T>
    where T : struct
{
    private readonly BufferResource<T>[] _resources;
    private volatile int _index;

    public FineLockingSkipBuffer(params BufferResource<T>[] resources)
    {
        if (resources.Length == 0)
            throw new ArgumentOutOfRangeException(
                nameof(resources),
                "There must be at least 1 resource");
        
        _index = -1; // Start at 0
        _resources = new BufferResource<T>[resources.Length];
        for (var i = 0; i < resources.Length; i++)
            _resources[i] = new BufferResource<T>(resources[i]);
    }

    public bool TryUpdate(int index, object? state = null)
    {
        var rsc = _resources[index];
        var locked = rsc.TryLock(ResourceAccessFlag.Write, out var hlock);
        if (!locked)
            return false;

        rsc.UpdaterState = state;
        rsc.UpdateResource();
        
        hlock.Dispose();
        return true;
    }

    public ResourceLockHandle GetNext(out T rscObject)
    {
        while (true)
        {
            // _index will eventually overflow and skip a few elements
            // for the sake of no locking on the index. It will happen very infrequently.
            // It's not undefined behavior
            var i = (uint)Interlocked.Increment(ref _index) % (uint)_resources.Length;
            var rsc = _resources[i];
            
            var locked = rsc.TryLock(ResourceAccessFlag.Write, out var hlock);
            if (!locked)
                continue;

            rscObject = rsc.Resource;
            return hlock;
        }
    }
}