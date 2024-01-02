namespace Buffering.Recycling;

public class ObjectPool<T> where T : class, IRecyclable
{
    private readonly Stack<T> _pool = new();
    public int Capacity { get; }
    private Func<T> _generator;

    public ObjectPool(Func<T> generator, int capacity, int preallocated = 0)
    {
        Capacity = capacity;
        _generator = generator;
        EnsurePoolContainsAtLeast(preallocated);
    }

    public void EnsurePoolContainsAtLeast(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Capacity);
        for (var i = _pool.Count; i < count; i++)
            TryPut(_generator());
    }

    public T Take()
    {
        if (!_pool.TryPeek(out var result))
            result = _generator();

        return result;
    }

    public void TakeRange(Span<T> span)
    {
        foreach (ref var o in span)
            o = Take();
    }

    public IEnumerable<T> TakeRange(int count)
    {
        while (count-- > 0)
            yield return Take();
    }

    public bool TryPut(T o)
    {
        if (_pool.Count >= Capacity)
            return false;
        
        o.Reset();
        _pool.Push(o);
        return true;
    }

    public bool TryPutRange(IEnumerable<T> range)
    {
        var anyFailed = false;
        foreach (var o in range)
        {
            anyFailed |= TryPut(o);
        }

        return anyFailed;
    }
}