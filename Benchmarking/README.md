# DoubleBuffer Performance Benchmarks

This benchmarking suite provides comprehensive side-by-side performance and allocation comparisons between the **current lock-free DoubleBuffer** implementation and the **old DoubleBuffer** from commit `4a9c72b4` (which used `StrongBox<T>`, locking abstractions `IResourceLock`, `ResourceLockHandle`, and metadata tracking).

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
