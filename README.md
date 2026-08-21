# Buffering

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Target Framework](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)
[![AOT Compatible](https://img.shields.io/badge/AOT-Compatible-success.svg)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

**Buffering** is a high-performance, low-contention double buffering library for .NET inspired by DirectX swap chains. It provides a clean, decoupled architecture for concurrent producer-consumer workflows, minimizing lock contention during front buffer reads and swaps.

---

## Key Features

- **Decoupled Producer/Consumer Architecture**: Separate `BackWriter` and `FrontReader` structs isolate read and write responsibilities.
- **Ultra-Low Lock Contention**: The back buffer is updated without locks on a dedicated writer thread. Swapping buffers is an instant reference flip under minimal lock duration.
- **Pluggable Locking Strategies**: Choose the optimal synchronization primitive for your workload (from modern `System.Threading.Lock` to `ReaderWriterLockSlim` or zero-overhead `NoLock`).
- **Metadata Tracking**: Built-in `BufferedResourceInfo` provides sequence IDs and status tracking to identify new frames/updates without manual diffing.
- **Native AOT & Trimming Ready**: Fully compatible with Native AOT and trimming in .NET 8 and .NET 10+.

---

## How It Works

Double buffering uses two internal buffers to allow concurrent reading and writing without blocking:

1. **Back Buffer (Producer)**: The background worker updates the back buffer independently without locking.
2. **Buffer Swap**: Calling `writer.SwapBuffers()` flips the references under a brief write lock.
3. **Front Buffer (Consumer)**: The consumer reads from the front buffer under a read lock handle that is released as soon as the read or copy operation completes.

```
+------------------+          SwapBuffers()          +-------------------+
|   Back Buffer    |  ============================>  |   Front Buffer    |
| (Updated by writer)                                | (Read by consumer)|
+------------------+                                 +-------------------+
```

---

## Locking Strategies

The library provides several `IResourceLock` implementations in `Buffering.Locking.Locks` to match your concurrency requirements:

| Lock Implementation | Underlying Mechanism | Recommended Use Case |
| :--- | :--- | :--- |
| `SystemThreadingLock` | `System.Threading.Lock` (.NET 10+) | Best for .NET 10+ applications with standard single-reader / single-writer workloads. |
| `MonitorLock` | `System.Threading.Monitor` | General-purpose lock for standard two-thread producer/consumer setups on all supported runtimes. |
| `MultipleReaderLock` | `System.Threading.ReaderWriterLockSlim` | Optimized for multiple consumer threads concurrently reading the front buffer. |
| `SpinnerLock` | `System.Threading.SpinLock` | Ultra-low-latency scenarios with extremely short read operations where thread yielding overhead is undesirable. |
| `NoLock` | No-op (zero overhead) | Single-threaded scenarios or systems where thread synchronization is handled externally. |

---

## Getting Started

### Example: Producer-Consumer Double Buffer

Below is a complete example demonstrating how to set up a double buffer, update data on a background worker thread, and read the latest available frame on a consumer thread:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Buffering.BufferResources;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;

// 1. Initialize double buffer with a lock strategy and swap effect
var doubleBuffer = new DoubleBuffer<string>(
    new MonitorLock(),
    DoubleBufferSwapEffect.Flip);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// 2. Start producer / writer thread (updates back buffer and swaps)
var producerTask = Task.Factory.StartNew(() =>
{
    var writer = doubleBuffer.BackWriter;
    var count = 0;

    while (!cts.Token.IsCancellationRequested)
    {
        // Produce new state
        writer.UpdateBackBuffer($"State #{++count}");

        // Swap back buffer into front buffer
        writer.SwapBuffers();

        Thread.Sleep(50); // Simulate work rate
    }
}, TaskCreationOptions.LongRunning);

// 3. Consumer / reader loop (reads front buffer)
var reader = doubleBuffer.FrontReader;
uint lastSeenId = 0;

while (!producerTask.IsCompleted)
{
    // Read the front buffer under lock
    using (reader.ReadFrontBuffer(out var data, out var info))
    {
        if (info.FromBuffer && info.Id != lastSeenId)
        {
            lastSeenId = info.Id;
            Console.WriteLine($"[Frame {info.Id}] Read: {data}");
        }
    }

    Thread.Sleep(20); // Consumer loop rate
}

await producerTask;
```

---

## Best Practices

- **Dispose Lock Handles Promptly**: Always dispose the `ResourceLockHandle` returned by `ReadFrontBuffer` as quickly as possible (e.g., using a `using` block or statement) to minimize wait times for buffer swaps.
- **Cache Reader and Writer Locally**: Obtain `FrontReader` and `BackWriter` once per thread/scope; these are lightweight `readonly struct` handles designed for low-overhead access.
- **Dedicated Producer Thread**: Use a dedicated thread or `TaskCreationOptions.LongRunning` for back buffer updates to achieve consistent throughput.

---

## License

This project is licensed under the [MIT License](LICENSE).
