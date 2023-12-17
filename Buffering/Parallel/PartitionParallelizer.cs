using System.Diagnostics;

namespace Buffering.Parallel;

public class PartitionParallelizer<T>
{
    public delegate void ParallelRefDataHandler(ref T o);
    
    public int Chunks { get; }
    public int ThreadCount { get; }
    public ParallelRefDataHandler DataHandler;
    private readonly Thread[] _threads;
    private readonly T[] _data;
    private readonly Range[] _chunkRanges;

    public PartitionParallelizer(int chunks, int threadCount, T[] data, ParallelRefDataHandler dataHandler)
    {
        Chunks = chunks;
        ThreadCount = threadCount;
        DataHandler = dataHandler;
        _data = data;
        _threads = new Thread[threadCount];
        _chunkRanges = _data.Partition(chunks);
        
        InitThreads();
    }

    private bool _threadsInitialized = false;
    private int _index = -1;
    private void InitThreads()
    {
        Debug.Assert(_index == -1 && !_threadsInitialized);
        
        if (ThreadCount == Chunks)
        {
            for (var i = 0; i < _threads.Length; i++)
            {
                var j = i;
                _threads[j] = new Thread(() =>
                {
                    var range = _chunkRanges[j];
                    foreach (ref var o in _data.AsSpan(range))
                    {
                        DataHandler.Invoke(ref o);
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
                        foreach (ref var o in _data.AsSpan(range))
                        {
                            DataHandler.Invoke(ref o);
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
        EnsureThreadInit();
        foreach (var t in _threads)
            t.Start();
    }

    public void Join()
    {
        EnsureThreadInit();
        foreach (var t in _threads)
            t.Join();
    }
}