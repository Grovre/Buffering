using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using Benchmarking.OldDoubleBuffering;

namespace Benchmarking.Benchmarks;

/// <summary>
/// Compares construction allocation and execution time between the old DoubleBuffer (commit 4a9c72b4)
/// and the current lock-free DoubleBuffer.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ConstructionBenchmark
{
    private readonly NoLock _noLock = new();
    private readonly MonitorLock _monitorLock = new();
    private readonly SpinnerLock _spinnerLock = new();
    private readonly SystemThreadingLock _systemThreadingLock = new();
    private readonly MultipleReaderLock _multipleReaderLock = new();

    [Benchmark(Baseline = true, Description = "Old DoubleBuffer (NoLock) Construction")]
    public OldDoubleBuffer<int> Old_NoLock_Create()
    {
        return new OldDoubleBuffer<int>(_noLock, OldDoubleBufferSwapEffect.Flip);
    }

    [Benchmark(Description = "Old DoubleBuffer (MonitorLock) Construction")]
    public OldDoubleBuffer<int> Old_MonitorLock_Create()
    {
        return new OldDoubleBuffer<int>(_monitorLock, OldDoubleBufferSwapEffect.Flip);
    }

    [Benchmark(Description = "Old DoubleBuffer (SpinnerLock) Construction")]
    public OldDoubleBuffer<int> Old_SpinnerLock_Create()
    {
        return new OldDoubleBuffer<int>(_spinnerLock, OldDoubleBufferSwapEffect.Flip);
    }

    [Benchmark(Description = "Old DoubleBuffer (SystemThreadingLock) Construction")]
    public OldDoubleBuffer<int> Old_SystemThreadingLock_Create()
    {
        return new OldDoubleBuffer<int>(_systemThreadingLock, OldDoubleBufferSwapEffect.Flip);
    }

    [Benchmark(Description = "Old DoubleBuffer (MultipleReaderLock) Construction")]
    public OldDoubleBuffer<int> Old_MultipleReaderLock_Create()
    {
        return new OldDoubleBuffer<int>(_multipleReaderLock, OldDoubleBufferSwapEffect.Flip);
    }

    [Benchmark(Description = "Current DoubleBuffer (LockFree) Construction")]
    public DoubleBuffer<int> Current_LockFree_Create()
    {
        return new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
    }
}
