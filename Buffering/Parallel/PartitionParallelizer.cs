using System.Diagnostics;

namespace Buffering.Parallel;

/// <summary>
/// Used to partition an array of data and process it in parallel.
/// All threads are new. All data is passed by reference from the array.
///
/// When chunk count and thread count differ, threads synchronize
/// on an interlocked index variable. When they are the same, threads
/// are initialized with their chunk only once with no synchronization.
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public sealed class PartitionParallelizer<T>
{
    /// <summary>
    /// Used to process some data on a thread
    /// </summary>
    public delegate void ParallelRefDataHandler(ref T o);
    
    /// <summary>
    /// Amount of chunks for threads to work on.
    /// The ideal chunk count equals thread count.
    /// </summary>
    public int Chunks { get; }
    /// <summary>
    /// Amount of threads to work on chunks.
    /// The ideal thread count equals chunk count.
    /// </summary>
    public int ThreadCount { get; }
    /// <summary>
    /// Delegate to process the data on any thread.
    /// </summary>
    public ParallelRefDataHandler DataHandler;
    private readonly Thread[] _threads;
    private readonly T[] _data;
    private readonly Range[] _chunkRanges;

    /// <summary>
    /// Initializes the PartitionParallelizer and the threads it will use based on the arguments.
    /// </summary>
    /// <param name="chunks">Amount of chunks</param>
    /// <param name="threadCount">Amount of threads</param>
    /// <param name="data">Data to partition/split into chunks</param>
    /// <param name="dataHandler">Action to handle one datum by reference.</param>
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

    /// <summary>
    /// Starts all threads and begins work on the partitioned data
    /// </summary>
    public void Start()
    {
        EnsureThreadInit();
        foreach (var t in _threads)
            t.Start();
    }

    /// <summary>
    /// Joins all threads, waiting for work to end on the partitioned data
    /// </summary>
    public void Join()
    {
        EnsureThreadInit();
        foreach (var t in _threads)
            t.Join();
    }
}