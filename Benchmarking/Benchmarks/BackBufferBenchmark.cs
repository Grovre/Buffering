using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using Benchmarking.Models;
using Benchmarking.OldDoubleBuffering;

namespace Benchmarking.Benchmarks;

/// <summary>
/// Compares back buffer write/read operations between the old DoubleBuffer (commit 4a9c72b4)
/// and the current lock-free DoubleBuffer.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(Vector3D))]
[GenericTypeArguments(typeof(PayloadClass))]
public class BackBufferBenchmark<T> where T : new()
{
    private OldDoubleBuffer<T> _oldBuffer = null!;
    private OldDoubleBufferBackWriter<T> _oldWriter;

    private DoubleBuffer<T> _currentBuffer = null!;
    private DoubleBufferBackWriter<T> _currentWriter = null!;

    private T _valueToUpdate = default!;

    [GlobalSetup]
    public void Setup()
    {
        _valueToUpdate = new T();

        _oldBuffer = new OldDoubleBuffer<T>(new NoLock(), OldDoubleBufferSwapEffect.Flip);
        _oldBuffer.UpdateBackBuffer(_valueToUpdate);
        _oldBuffer.SwapBuffers();
        _oldBuffer.UpdateBackBuffer(_valueToUpdate);
        _oldWriter = _oldBuffer.BackWriter;

        _currentBuffer = new DoubleBuffer<T>(_valueToUpdate, _valueToUpdate, DoubleBufferSwapEffect.FlipRefOrValue);
        _currentWriter = _currentBuffer.BackWriter;
    }

    [Benchmark(Baseline = true, Description = "Old DoubleBuffer UpdateBackBuffer")]
    public void Old_UpdateBackBuffer()
    {
        _oldWriter.UpdateBackBuffer(in _valueToUpdate);
    }

    [Benchmark(Description = "Current DoubleBuffer UpdateBackBuffer")]
    public void Current_UpdateBackBuffer()
    {
        _currentWriter.UpdateBackBuffer(_valueToUpdate);
    }

    [Benchmark(Description = "Old DoubleBuffer ReadBackBuffer (ref)")]
    public ref T Old_ReadBackBuffer()
    {
        return ref _oldWriter.ReadBackBuffer();
    }

    [Benchmark(Description = "Current DoubleBuffer ReadBackBuffer (val)")]
    public T Current_ReadBackBuffer()
    {
        return _currentWriter.ReadBackBuffer();
    }
}
