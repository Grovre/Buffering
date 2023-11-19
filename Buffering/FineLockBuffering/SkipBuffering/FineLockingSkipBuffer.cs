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
        _resources = new BufferResource<T, TUpdaterState>[resources.Length];
        for (var i = 0; i < resources.Length; i++)
            _resources[i] = new BufferResource<T, TUpdaterState>(resources[i]);
        _infos = new BufferedResourceInfo[_resources.Length];
    }

    public bool TryUpdate(int index, TUpdaterState state)
    {
        var rsc = _resources[index];
        var locked = rsc.TryLock(ResourceAccessFlag.Write, out var hlock);
        if (!locked)
            return false;

        rsc.UpdaterState = state;
        rsc.UpdateResource();
        _infos[index] = BufferedResourceInfo.PrepareNextInfo(_infos[index], true);
        
        hlock.Dispose();
        return true;
    }

    public ResourceLockHandle GetNext(out T rscObject, out BufferedResourceInfo info)
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
            info = _infos[i];
            
            return hlock;
        }
    }
}