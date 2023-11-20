using Buffering.BufferResources;
using Buffering.Locking;

namespace Buffering.FineLockBuffering.SkipBuffering;

public class FineLockingSkipBuffer<T, TUpdaterState>
    where T : struct
{
    private readonly BufferedResourceInfo[] _infos;
    private readonly BufferResource<T, TUpdaterState>[] _resources;
    private volatile int _index;

    public FineLockingSkipBuffer(params BufferResource<T, TUpdaterState>[] resources)
    {
        if (resources.Length == 0)
            throw new ArgumentOutOfRangeException(
                nameof(resources),
                "There must be at least 1 resource");
        
        _index = -1; // Start at 0
        _resources = resources;
        _infos = new BufferedResourceInfo[_resources.Length];
    }

    private ResourceLockHandle LockNextAvailableBuffer(out int index)
    {
        while (true)
        {
            // _index will eventually overflow and skip a few elements
            // for the sake of allowing parallel readings without locking
            // on the index to increment and set back to 0 at capacity.
            // It's all atomic. It's not undefined behavior
            var i = (int)((uint)Interlocked.Increment(ref _index) % (uint)_resources.Length);
            var rsc = _resources[i];
            
            var locked = rsc.TryLock(ResourceAccessFlag.Write, out var hlock);
            if (!locked)
                continue;

            index = i;
            return hlock;
        }
    }

    public void UpdateNextUnlockedBuffer(TUpdaterState state)
    {
        using var hlock = LockNextAvailableBuffer(out var i);
        _resources[i].UpdateResource(state);
        _infos[i] = BufferedResourceInfo.PrepareNextInfo(_infos[i], true);
    }

    public bool TryUpdate(int index, TUpdaterState state)
    {
        var rsc = _resources[index];
        var locked = rsc.TryLock(ResourceAccessFlag.Write, out var hlock);
        if (!locked)
            return false;

        rsc.UpdateResource(state);
        _infos[index] = BufferedResourceInfo.PrepareNextInfo(_infos[index], true);
        
        hlock.Dispose();
        return true;
    }

    public ResourceLockHandle ReadNextUnlockedBuffer(out T rscObject, out BufferedResourceInfo info)
    {
        var hlock = LockNextAvailableBuffer(out var i);
        rscObject = _resources[i].Resource;
        info = _infos[i];
        return hlock;
    }
}