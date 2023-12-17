using System.Diagnostics;

namespace Buffering.Parallel;

public class PartitionParallelizer<T>
{
    public int Chunks { get; }
    public int ThreadCount { get; }
    public Action<T> DataAction;
    private readonly Thread[] _threads;
    private readonly T[] _data;
    private readonly Range[] _chunkRanges;

    public PartitionParallelizer(int chunks, int threadCount, T[] data, Action<T> dataAction)
    {
        Chunks = chunks;
        ThreadCount = threadCount;
        DataAction = dataAction;
        _data = data;
        _threads = new Thread[threadCount];
        _chunkRanges = _data.Partition(chunks);
        
        InitThreads();
    }

    private bool _threadsInitialized = false;
    private int _index = -1;
    private void InitThreads()
    {
        Debug.Assert(_index < 0 && !_threadsInitialized);
        
        if (ThreadCount == Chunks)
        {
            for (var i = 0; i < _threads.Length; i++)
            {
                var j = i;
                _threads[j] = new Thread(() =>
                {
                    var range = _chunkRanges[j];
                    foreach (var o in _data.AsSpan(range))
                    {
                        DataAction.Invoke(o);
                    }
                });
            }
        }
        else
        {
            foreach (ref var t in _threads.AsSpan())
            {
                t = new Thread(() =>
                {
                    var i = Interlocked.Increment(ref _index);
                    while (i < _chunkRanges.Length)
                    {
                        var range = _chunkRanges[i];
                        foreach (var o in _data.AsSpan(range))
                        {
                            DataAction.Invoke(o);
                        }

                        i = Interlocked.Increment(ref _index);
                    }
                });
            }
        }
        
        foreach (var t in _threads)
        {
            t.Name = "Partition Parallelizer Thread";
        }

        _threadsInitialized = true;
    }

    private void EnsureThreadInit()
    {
        if (!_threadsInitialized)
            throw new ApplicationException("Threads not initialized");
    }

    public void Start()
    {
        foreach (var t in _threads)
            t.Start();
    }

    public void Join()
    {
        foreach (var t in _threads)
            t.Join();
    }
}