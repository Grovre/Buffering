using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using Benchmarking.Models;
using Benchmarking.OldDoubleBuffering;

namespace Benchmarking.Benchmarks;

/// <summary>
/// Compares the producer cycle (UpdateBackBuffer + SwapBuffers) between the old DoubleBuffer
/// (commit 4a9c72b4) across various locking mechanisms and the current lock-free DoubleBuffer.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(Vector3D))]
[GenericTypeArguments(typeof(PayloadClass))]
public class ProducerCycleBenchmark<T> where T : new()
{
    private OldDoubleBuffer<T> _oldNoLock = null!;
    private OldDoubleBufferBackWriter<T> _oldNoLockWriter;

    private OldDoubleBuffer<T> _oldMonitor = null!;
    private OldDoubleBufferBackWriter<T> _oldMonitorWriter;

    private OldDoubleBuffer<T> _oldSpinner = null!;
    private OldDoubleBufferBackWriter<T> _oldSpinnerWriter;

    private OldDoubleBuffer<T> _oldSystemThreading = null!;
    private OldDoubleBufferBackWriter<T> _oldSystemThreadingWriter;

    private OldDoubleBuffer<T> _oldMultipleReader = null!;
    private OldDoubleBufferBackWriter<T> _oldMultipleReaderWriter;

    private DoubleBuffer<T> _currentFlip = null!;
    private DoubleBufferBackWriter<T> _currentFlipWriter = null!;

    private DoubleBuffer<T> _currentCopy = null!;
    private DoubleBufferBackWriter<T> _currentCopyWriter = null!;

    private T _sampleValue = default!;

    [GlobalSetup]
    public void Setup()
    {
        _sampleValue = new T();

        _oldNoLock = new OldDoubleBuffer<T>(new NoLock(), OldDoubleBufferSwapEffect.Flip);
        _oldNoLockWriter = _oldNoLock.BackWriter;

        _oldMonitor = new OldDoubleBuffer<T>(new MonitorLock(), OldDoubleBufferSwapEffect.Flip);
        _oldMonitorWriter = _oldMonitor.BackWriter;

        _oldSpinner = new OldDoubleBuffer<T>(new SpinnerLock(), OldDoubleBufferSwapEffect.Flip);
        _oldSpinnerWriter = _oldSpinner.BackWriter;

        _oldSystemThreading = new OldDoubleBuffer<T>(new SystemThreadingLock(), OldDoubleBufferSwapEffect.Flip);
        _oldSystemThreadingWriter = _oldSystemThreading.BackWriter;

        _oldMultipleReader = new OldDoubleBuffer<T>(new MultipleReaderLock(), OldDoubleBufferSwapEffect.Flip);
        _oldMultipleReaderWriter = _oldMultipleReader.BackWriter;

        _currentFlip = new DoubleBuffer<T>(_sampleValue, _sampleValue, DoubleBufferSwapEffect.FlipRefOrValue);
        _currentFlipWriter = _currentFlip.BackWriter;

        _currentCopy = new DoubleBuffer<T>(_sampleValue, _sampleValue, DoubleBufferSwapEffect.CopyRefOrValue);
        _currentCopyWriter = _currentCopy.BackWriter;
    }

    [Benchmark(Baseline = true, Description = "Old DoubleBuffer (NoLock) Update & Swap")]
    public void Old_NoLock_UpdateAndSwap()
    {
        _oldNoLockWriter.UpdateBackBuffer(in _sampleValue);
        _oldNoLockWriter.SwapBuffers();
    }

    [Benchmark(Description = "Old DoubleBuffer (MonitorLock) Update & Swap")]
    public void Old_MonitorLock_UpdateAndSwap()
    {
        _oldMonitorWriter.UpdateBackBuffer(in _sampleValue);
        _oldMonitorWriter.SwapBuffers();
    }

    [Benchmark(Description = "Old DoubleBuffer (SpinnerLock) Update & Swap")]
    public void Old_SpinnerLock_UpdateAndSwap()
    {
        _oldSpinnerWriter.UpdateBackBuffer(in _sampleValue);
        _oldSpinnerWriter.SwapBuffers();
    }

    [Benchmark(Description = "Old DoubleBuffer (SystemThreadingLock) Update & Swap")]
    public void Old_SystemThreadingLock_UpdateAndSwap()
    {
        _oldSystemThreadingWriter.UpdateBackBuffer(in _sampleValue);
        _oldSystemThreadingWriter.SwapBuffers();
    }

    [Benchmark(Description = "Old DoubleBuffer (MultipleReaderLock) Update & Swap")]
    public void Old_MultipleReaderLock_UpdateAndSwap()
    {
        _oldMultipleReaderWriter.UpdateBackBuffer(in _sampleValue);
        _oldMultipleReaderWriter.SwapBuffers();
    }

    [Benchmark(Description = "Current DoubleBuffer (FlipRefOrValue) Update & Swap")]
    public bool Current_FlipRefOrValue_UpdateAndSwap()
    {
        _currentFlipWriter.UpdateBackBuffer(_sampleValue);
        return _currentFlipWriter.SwapBuffers();
    }

    [Benchmark(Description = "Current DoubleBuffer (CopyRefOrValue) Update & Swap")]
    public bool Current_CopyRefOrValue_UpdateAndSwap()
    {
        _currentCopyWriter.UpdateBackBuffer(_sampleValue);
        return _currentCopyWriter.SwapBuffers();
    }
}
