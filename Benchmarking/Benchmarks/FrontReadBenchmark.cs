using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using Benchmarking.Models;
using Benchmarking.OldDoubleBuffering;

namespace Benchmarking.Benchmarks;

/// <summary>
/// Compares the front buffer read performance of the current lock-free DoubleBuffer
/// against the old DoubleBuffer (commit 4a9c72b4) across various locking strategies.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(Vector3D))]
[GenericTypeArguments(typeof(PayloadClass))]
public class FrontReadBenchmark<T> where T : new()
{
    private OldDoubleBuffer<T> _oldNoLock = null!;
    private OldDoubleBufferFrontReader<T> _oldNoLockReader;

    private OldDoubleBuffer<T> _oldMonitor = null!;
    private OldDoubleBufferFrontReader<T> _oldMonitorReader;

    private OldDoubleBuffer<T> _oldSpinner = null!;
    private OldDoubleBufferFrontReader<T> _oldSpinnerReader;

    private OldDoubleBuffer<T> _oldSystemThreading = null!;
    private OldDoubleBufferFrontReader<T> _oldSystemThreadingReader;

    private OldDoubleBuffer<T> _oldMultipleReader = null!;
    private OldDoubleBufferFrontReader<T> _oldMultipleReaderReader;

    private DoubleBuffer<T> _current = null!;
    private DoubleBufferFrontReader<T> _currentReader = null!;

    [GlobalSetup]
    public void Setup()
    {
        var initVal = new T();

        _oldNoLock = new OldDoubleBuffer<T>(new NoLock(), OldDoubleBufferSwapEffect.Flip);
        _oldNoLock.UpdateBackBuffer(initVal);
        _oldNoLock.SwapBuffers();
        _oldNoLockReader = _oldNoLock.FrontReader;

        _oldMonitor = new OldDoubleBuffer<T>(new MonitorLock(), OldDoubleBufferSwapEffect.Flip);
        _oldMonitor.UpdateBackBuffer(initVal);
        _oldMonitor.SwapBuffers();
        _oldMonitorReader = _oldMonitor.FrontReader;

        _oldSpinner = new OldDoubleBuffer<T>(new SpinnerLock(), OldDoubleBufferSwapEffect.Flip);
        _oldSpinner.UpdateBackBuffer(initVal);
        _oldSpinner.SwapBuffers();
        _oldSpinnerReader = _oldSpinner.FrontReader;

        _oldSystemThreading = new OldDoubleBuffer<T>(new SystemThreadingLock(), OldDoubleBufferSwapEffect.Flip);
        _oldSystemThreading.UpdateBackBuffer(initVal);
        _oldSystemThreading.SwapBuffers();
        _oldSystemThreadingReader = _oldSystemThreading.FrontReader;

        _oldMultipleReader = new OldDoubleBuffer<T>(new MultipleReaderLock(), OldDoubleBufferSwapEffect.Flip);
        _oldMultipleReader.UpdateBackBuffer(initVal);
        _oldMultipleReader.SwapBuffers();
        _oldMultipleReaderReader = _oldMultipleReader.FrontReader;

        _current = new DoubleBuffer<T>(initVal, initVal, DoubleBufferSwapEffect.FlipRefOrValue);
        _currentReader = _current.FrontReader;
    }

    [Benchmark(Baseline = true, Description = "Old DoubleBuffer (NoLock)")]
    public T Old_NoLock()
    {
        using var h = _oldNoLockReader.ReadFrontBuffer(out var rsc, out _);
        return rsc;
    }

    [Benchmark(Description = "Old DoubleBuffer (MonitorLock)")]
    public T Old_MonitorLock()
    {
        using var h = _oldMonitorReader.ReadFrontBuffer(out var rsc, out _);
        return rsc;
    }

    [Benchmark(Description = "Old DoubleBuffer (SpinnerLock)")]
    public T Old_SpinnerLock()
    {
        using var h = _oldSpinnerReader.ReadFrontBuffer(out var rsc, out _);
        return rsc;
    }

    [Benchmark(Description = "Old DoubleBuffer (SystemThreadingLock)")]
    public T Old_SystemThreadingLock()
    {
        using var h = _oldSystemThreadingReader.ReadFrontBuffer(out var rsc, out _);
        return rsc;
    }

    [Benchmark(Description = "Old DoubleBuffer (MultipleReaderLock)")]
    public T Old_MultipleReaderLock()
    {
        using var h = _oldMultipleReaderReader.ReadFrontBuffer(out var rsc, out _);
        return rsc;
    }

    [Benchmark(Description = "Current DoubleBuffer (LockFree)")]
    public T Current_LockFree()
    {
        return _currentReader.ReadFrontBuffer();
    }
}
