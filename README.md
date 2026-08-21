# Buffering

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)
[![AOT Compatible](https://img.shields.io/badge/AOT-Compatible-success.svg)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Lock-Free](https://img.shields.io/badge/Concurrency-Lock--Free%20SWMR-brightgreen.svg)](#concurrency-model)
[![NuGet](https://img.shields.io/nuget/v/Buffering.svg)](https://www.nuget.org/packages/Buffering)
[![GitHub](https://img.shields.io/badge/GitHub-Grovre%2FBuffering-black?logo=github)](https://github.com/Grovre/Buffering)

A lock-free double buffering library for .NET, built around a single-writer, multiple-reader (SWMR) concurrency model. One producer thread stages updates into a private back buffer while any number of consumer threads read from the published front buffer — no locks, no blocking, and no allocations within the buffer.

Common use cases include game loops, audio processing, telemetry pipelines, and any scenario where a producer generates data at its own pace and consumers need access to the latest available version without contending with the writer or each other.

---

## How It Works
```
 Producer (single writer)                        Consumers (multiple readers)
+--------------------------+                    +----------------------------+
| DoubleBufferBackWriter<T>|                    | DoubleBufferFrontReader<T> |
+--------------------------+                    +----------------------------+
            |                                                |
            | UpdateBackBuffer(data)                         | ReadFrontBuffer()
            v                                                v
   [  Back Buffer Slot  ]                          [  Front Buffer Slot  ]
   |  Private Staging   |                          |  Active Published   |
   +---------------------+                         +---------------------+
            |                                                ^
            | SwapBuffers()                                  |
            +==================== Memory Barrier ============+
                                Publication
```

1. **Staging** — The producer writes new data to the back buffer via `UpdateBackBuffer`. The front buffer is untouched and readers continue seeing the previous value.
2. **Publication** — `SwapBuffers` moves the staged data to the front buffer and issues a memory barrier, making it visible to all cores.
3. **Reading** — Each consumer calls `ReadFrontBuffer` to get the latest published value. Reads are concurrent and never block.

---

## Swap Effects

The `DoubleBufferSwapEffect` enum controls what happens to the old front buffer when a swap occurs:

| Swap Effect      | Operation                           | Value Types (`struct`) | Reference Types (`class`)                          | Best For                                                                          |
| :--------------- | :---------------------------------- | :--------------------- | :------------------------------------------------- | :-------------------------------------------------------------------------------- |
| `FlipRefOrValue` | `(_front, _back) = (_back, _front)` | Values exchanged       | References exchanged                               | Ping-pong recycling — reuse the old front buffer object as the next back buffer   |
| `CopyRefOrValue` | `_front = _back`                    | Value copied           | Reference copied (both slots point to same object) | Persistent baseline state — back buffer retains its value for incremental updates |

`FlipRefOrValue` is recommended for most cases. With reference types, it enables zero-allocation recycling: after a swap, the old front buffer object becomes the new back buffer. The writer can retrieve it via `ReadBackBuffer`, mutate it in place, and swap again — no heap allocations between frames.

`CopyRefOrValue` is useful when each update builds on the previous one (e.g., incremental simulation state, cumulative counters). The back buffer retains its value after the swap, so the writer can continue accumulating into it.

---

## Quick Start

```csharp
using Buffering.DoubleBuffering;

var buffer = new DoubleBuffer<Guid>(
    Guid.Empty,
    Guid.Empty,
    DoubleBufferSwapEffect.FlipRefOrValue);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// Producer thread
var producer = Task.Run(() =>
{
    var writer = buffer.BackWriter;

    while (!cts.IsCancellationRequested)
    {
        writer.UpdateBackBuffer(Guid.NewGuid());
        writer.SwapBuffers();
        Thread.Sleep(16); // ~60 FPS
    }
});

// Consumer (can run on any number of threads)
var reader = buffer.FrontReader;
var lastSeen = Guid.Empty;

while (!producer.IsCompleted)
{
    var current = reader.ReadFrontBuffer();

    if (current != lastSeen)
    {
        lastSeen = current;
        Console.WriteLine($"Received: {current}");
    }

    Thread.Sleep(8);
}

await producer;
```

---

## Examples

### Zero-Allocation Frame Recycling

Pre-allocate two buffer objects and ping-pong between them to avoid GC pressure in hot loops:

```csharp
public class FrameData
{
    public byte[] Pixels { get; } = new byte[1920 * 1080 * 4];
    public long Timestamp { get; set; }
}

var buffer = new DoubleBuffer<FrameData>(
    new FrameData(),
    new FrameData(),
    DoubleBufferSwapEffect.FlipRefOrValue);

var writer = buffer.BackWriter;

while (capturing)
{
    // Get the recycled frame (old front buffer, now in back)
    var frame = writer.ReadBackBuffer();

    // Populate in place — zero allocations
    CaptureCameraFrameInto(frame.Pixels);
    frame.Timestamp = Stopwatch.GetTimestamp();

    // Mark as updated and publish
    writer.UpdateBackBuffer(frame);
    writer.SwapBuffers();
}
```

### Incremental State Accumulation

When each update builds on the previous state, `CopyRefOrValue` lets the back buffer retain its value across swaps:

```csharp
public struct WorldState
{
    public int EntityCount;
    public float SimulationTime;
}

var buffer = new DoubleBuffer<WorldState>(
    default,
    default,
    DoubleBufferSwapEffect.CopyRefOrValue);

var writer = buffer.BackWriter;

var state = writer.ReadBackBuffer();
state.EntityCount += 5;
state.SimulationTime += 0.016f;

writer.UpdateBackBuffer(state);
writer.SwapBuffers(); // Front gets updated copy, back retains state for next tick
```

### Multiple Concurrent Readers

Any number of threads can read simultaneously without contention:

```csharp
var feed = new DoubleBuffer<MarketSnapshot>(
    initialSnapshot,
    initialSnapshot,
    DoubleBufferSwapEffect.FlipRefOrValue);

var uiTask = Task.Run(() => RenderUI(feed.FrontReader));
var riskTask = Task.Run(() => EvaluateRisk(feed.FrontReader));
var telemetryTask = Task.Run(() => StreamTelemetry(feed.FrontReader));
```

---

## Concurrency Model

| Rule                 | Guarantee                                                                                                                                                   |
| :------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Single writer**    | Only one thread may call `UpdateBackBuffer` and `SwapBuffers`. Concurrent writes from multiple threads will corrupt internal state.                         |
| **Multiple readers** | Any number of threads may call `ReadFrontBuffer` concurrently without synchronization.                                                                      |
| **Memory ordering**  | Both reads and swaps issue full memory barriers (`Interlocked.MemoryBarrier`) to ensure visibility across CPU cores on all architectures, including ARM64. |
| **Swap elision**     | `SwapBuffers` returns `false` if no `UpdateBackBuffer` call has been made since the last swap. The front buffer is left unchanged.                         |

### Read semantics

Each call to `ReadFrontBuffer` returns the most recently published value at the time of the call. Two readers that call at different times — with a swap in between — will see different values. This is by design: the buffer is a latest-value primitive, not a broadcast mechanism. If your scenario requires every reader to see every published value, use a queue or channel instead.

### Object ownership

The buffer does not assume ownership of `T`. If `T` implements `IDisposable`, disposal is the caller's responsibility. Use `ReadBackBuffer` to retrieve references for cleanup.

---

## Things to Watch Out For

### Mutating recycled reference types

The buffer synchronizes object *references*, not the contents of those objects. With `FlipRefOrValue`, the writer retrieves the old front buffer via `ReadBackBuffer` and mutates it in place. If a reader still holds a reference to that same object from a previous `ReadFrontBuffer` call, the writer's mutations will cause a data race.

To prevent data races:
- **Ephemeral consumption**: Readers should finish processing within their consumption cycle without holding long-lived references.
- **Snapshotting / Immutability**: If state needs to persist across frames, copy or snapshot the required fields, or use immutable types.
- **External synchronization**: If readers must hold references across frames while the writer mutates recycled objects, synchronize access to the shared instance using an external lock (such as `Monitor`, `ReaderWriterLockSlim`, or a custom locking handle).

### Reference aliasing with `CopyRefOrValue`

When `T` is a reference type, `CopyRefOrValue` sets `_front = _back` — both slots point to the same object. Mutating that object through the back buffer directly modifies what readers see through the front buffer, with no isolation.

To avoid aliasing hazards:
- Assign a new instance when staging updates rather than mutating the existing instance in place.
- Use immutable data types so neither reader nor writer mutates the shared instance.
- Coordinate reads and writes with external synchronization if shared mutable instances must be modified.

### Forgetting `UpdateBackBuffer` after in-place mutation

`ReadBackBuffer` returns the back buffer reference but does not mark it as pending an update. If you retrieve an object, mutate it, and call `SwapBuffers` without first calling `UpdateBackBuffer`, the swap is a no-op that returns `false`. Always call `UpdateBackBuffer` after preparing data, even if the reference hasn't changed.

### Struct tearing

Value types larger than the native pointer width (64 bits on x64) are not copied atomically. If a swap occurs while a reader is in the middle of reading a large struct, the reader may observe a mix of old and new field values. For torn-read-free guarantees, wrap large data in a reference type, keep structs within register width, or protect reads and writes with external synchronization.

### Single-writer violation

`DoubleBufferBackWriter<T>` is strictly single-writer. Calling `UpdateBackBuffer` or `SwapBuffers` from multiple threads without external synchronization will corrupt internal state. If you need multiple writers, coordinate them with a lock outside the buffer.

---

## Best Practices

- **Cache handles locally** — Store `FrontReader` and `BackWriter` in local variables or fields rather than accessing the properties repeatedly in hot loops.
- **Use a dedicated producer thread** — Run the write loop on a dedicated thread or `TaskCreationOptions.LongRunning` task for deterministic pacing.
- **Call `UpdateBackBuffer` after in-place mutation** — Even if the reference hasn't changed, the pending-update flag must be set or `SwapBuffers` will no-op.
- **Treat reader references as ephemeral** — When `T` is a reference type, consume or snapshot the data immediately rather than holding the reference across frames.
- **Check the return of `SwapBuffers`** — The `bool` return tells you whether a swap actually occurred, useful for conditional staging logic.

---

## Compatibility

- .NET 8.0 and 10.0+
- Native AOT and trimming compatible
- MIT licensed
