using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using Benchmarking.OldDoubleBuffering;

namespace Benchmarking.Benchmarks;

/// <summary>
/// Compares concurrent Single-Writer Multiple-Reader (SWMR) throughput and contention
/// between the old DoubleBuffer (commit 4a9c72b4) using various lock implementations
/// and the current lock-free DoubleBuffer.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ConcurrentBenchmark
{
    [Params(1, 4)]
    public int ReaderCount { get; set; }

    private const int OperationsPerRun = 2_000;

    [Benchmark(Baseline = true, Description = "Old DoubleBuffer (MonitorLock) SWMR")]
    public long Old_MonitorLock_SWMR()
    {
        var db = new OldDoubleBuffer<int>(new MonitorLock(), OldDoubleBufferSwapEffect.Flip);
        var writer = db.BackWriter;
        var reader = db.FrontReader;
        using var cts = new CancellationTokenSource();
        long totalReads = 0;

        var readers = new Task[ReaderCount];
        for (int i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(() =>
            {
                long localReads = 0;
                while (!cts.IsCancellationRequested)
                {
                    using var h = reader.ReadFrontBuffer(out var val, out _);
                    localReads += val;
                }
                Interlocked.Add(ref totalReads, localReads);
            });
        }

        for (int i = 0; i < OperationsPerRun; i++)
        {
            writer.UpdateBackBuffer(in i);
            writer.SwapBuffers();
        }

        cts.Cancel();
        Task.WaitAll(readers);
        return totalReads;
    }

    [Benchmark(Description = "Old DoubleBuffer (SpinnerLock) SWMR")]
    public long Old_SpinnerLock_SWMR()
    {
        var db = new OldDoubleBuffer<int>(new SpinnerLock(), OldDoubleBufferSwapEffect.Flip);
        var writer = db.BackWriter;
        var reader = db.FrontReader;
        using var cts = new CancellationTokenSource();
        long totalReads = 0;

        var readers = new Task[ReaderCount];
        for (int i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(() =>
            {
                long localReads = 0;
                while (!cts.IsCancellationRequested)
                {
                    using var h = reader.ReadFrontBuffer(out var val, out _);
                    localReads += val;
                }
                Interlocked.Add(ref totalReads, localReads);
            });
        }

        for (int i = 0; i < OperationsPerRun; i++)
        {
            writer.UpdateBackBuffer(in i);
            writer.SwapBuffers();
        }

        cts.Cancel();
        Task.WaitAll(readers);
        return totalReads;
    }

    [Benchmark(Description = "Old DoubleBuffer (MultipleReaderLock) SWMR")]
    public long Old_MultipleReaderLock_SWMR()
    {
        var db = new OldDoubleBuffer<int>(new MultipleReaderLock(), OldDoubleBufferSwapEffect.Flip);
        var writer = db.BackWriter;
        var reader = db.FrontReader;
        using var cts = new CancellationTokenSource();
        long totalReads = 0;

        var readers = new Task[ReaderCount];
        for (int i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(() =>
            {
                long localReads = 0;
                while (!cts.IsCancellationRequested)
                {
                    using var h = reader.ReadFrontBuffer(out var val, out _);
                    localReads += val;
                }
                Interlocked.Add(ref totalReads, localReads);
            });
        }

        for (int i = 0; i < OperationsPerRun; i++)
        {
            writer.UpdateBackBuffer(in i);
            writer.SwapBuffers();
        }

        cts.Cancel();
        Task.WaitAll(readers);
        return totalReads;
    }

    [Benchmark(Description = "Old DoubleBuffer (SystemThreadingLock) SWMR")]
    public long Old_SystemThreadingLock_SWMR()
    {
        var db = new OldDoubleBuffer<int>(new SystemThreadingLock(), OldDoubleBufferSwapEffect.Flip);
        var writer = db.BackWriter;
        var reader = db.FrontReader;
        using var cts = new CancellationTokenSource();
        long totalReads = 0;

        var readers = new Task[ReaderCount];
        for (int i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(() =>
            {
                long localReads = 0;
                while (!cts.IsCancellationRequested)
                {
                    using var h = reader.ReadFrontBuffer(out var val, out _);
                    localReads += val;
                }
                Interlocked.Add(ref totalReads, localReads);
            });
        }

        for (int i = 0; i < OperationsPerRun; i++)
        {
            writer.UpdateBackBuffer(in i);
            writer.SwapBuffers();
        }

        cts.Cancel();
        Task.WaitAll(readers);
        return totalReads;
    }

    [Benchmark(Description = "Current DoubleBuffer (LockFree) SWMR")]
    public long Current_LockFree_SWMR()
    {
        var db = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = db.BackWriter;
        var reader = db.FrontReader;
        using var cts = new CancellationTokenSource();
        long totalReads = 0;

        var readers = new Task[ReaderCount];
        for (int i = 0; i < ReaderCount; i++)
        {
            readers[i] = Task.Run(() =>
            {
                long localReads = 0;
                while (!cts.IsCancellationRequested)
                {
                    var val = reader.ReadFrontBuffer();
                    localReads += val;
                }
                Interlocked.Add(ref totalReads, localReads);
            });
        }

        for (int i = 0; i < OperationsPerRun; i++)
        {
            writer.UpdateBackBuffer(i);
            writer.SwapBuffers();
        }

        cts.Cancel();
        Task.WaitAll(readers);
        return totalReads;
    }
}
