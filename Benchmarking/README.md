# DoubleBuffer Performance Benchmarks

This benchmarking suite provides comprehensive side-by-side performance and allocation comparisons between the **current lock-free DoubleBuffer** implementation and the **old DoubleBuffer** from commit `4a9c72b4` (which used `StrongBox<T>`, locking abstractions `IResourceLock`, `ResourceLockHandle`, and metadata tracking).

## Benchmark Results

### Producer Update & Swap Performance

| Method                                                 | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| 'Current DoubleBuffer (CopyRefOrValue) Update & Swap'  |  4.151 ns | 0.0754 ns | 0.0589 ns |  4.171 ns |  0.63 |    0.03 |         - |          NA |
| 'Current DoubleBuffer (FlipRefOrValue) Update & Swap'  |  6.032 ns | 0.0949 ns | 0.0841 ns |  6.004 ns |  0.92 |    0.04 |         - |          NA |
| 'Old DoubleBuffer (NoLock) Update & Swap'              |  6.573 ns | 0.1534 ns | 0.2727 ns |  6.569 ns |  1.00 |    0.06 |         - |          NA |
| 'Old DoubleBuffer (SpinnerLock) Update & Swap'         |  7.871 ns | 0.1778 ns | 0.2183 ns |  7.777 ns |  1.20 |    0.06 |         - |          NA |
| 'Old DoubleBuffer (SystemThreadingLock) Update & Swap' | 10.126 ns | 0.2177 ns | 0.3051 ns |  9.998 ns |  1.54 |    0.08 |         - |          NA |
| 'Old DoubleBuffer (MonitorLock) Update & Swap'         | 11.796 ns | 0.2576 ns | 0.4444 ns | 11.647 ns |  1.80 |    0.10 |         - |          NA |
| 'Old DoubleBuffer (MultipleReaderLock) Update & Swap'  | 14.270 ns | 0.3081 ns | 0.5935 ns | 13.968 ns |  2.17 |    0.13 |         - |          NA |

## Benchmark Suites

1. **`FrontReadBenchmark<T>`**
   - Compares front buffer read throughput across different locking strategies (`NoLock`, `MonitorLock`, `SpinnerLock`, `SystemThreadingLock`, `MultipleReaderLock`) vs the current lock-free memory barrier implementation.
   - Tested across 3 data types: primitive `int`, 24-byte value type `Vector3D`, and reference type `PayloadClass`.

2. **`ProducerCycleBenchmark<T>`**
   - Compares the complete producer update-and-swap pipeline (`UpdateBackBuffer` + `SwapBuffers`) between the old lock-based DoubleBuffer and current lock-free DoubleBuffer (`FlipRefOrValue` and `CopyRefOrValue`).
   - Tested across primitive, struct, and class types.

3. **`BackBufferBenchmark<T>`**
   - Compares isolated back buffer update (`UpdateBackBuffer`) and back buffer read (`ReadBackBuffer`) operations between old and current implementations.

4. **`ConstructionBenchmark`**
   - Measures instantiation time and memory allocations (`[MemoryDiagnoser]`) when creating DoubleBuffer instances across all lock types vs current lock-free constructor.

5. **`ConcurrentBenchmark`**
   - Evaluates multi-threaded Single-Writer Multiple-Reader (SWMR) throughput and lock contention under real concurrent load with 1 writer thread and varying reader threads (`ReaderCount = 1, 4`).

## How to Run

To run all benchmarks (interactive prompt or filter):
```bash
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*"
```

To run a specific benchmark class:
```bash
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*FrontReadBenchmark*"
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*ProducerCycleBenchmark*"
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*ConstructionBenchmark*"
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*ConcurrentBenchmark*"
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*BackBufferBenchmark*"
```

For faster development runs:
```bash
dotnet run --project Benchmarking/Benchmarking.csproj -c Release -- --filter "*ConstructionBenchmark*" --job Short
```
