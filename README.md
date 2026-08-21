# Buffering

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Target Framework](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)
[![AOT Compatible](https://img.shields.io/badge/AOT-Compatible-success.svg)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Lock-Free](https://img.shields.io/badge/Concurrency-Lock--Free%20SWMR-brightgreen.svg)](#concurrency-model)

**Buffering** is an ultra-high-performance, lock-free double buffering library for .NET inspired by DirectX swap chains. Engineered for demanding real-time systems—such as game loops, audio processing engines, telemetry pipelines, and high-frequency data feeds—it decouples producers from consumers to deliver maximum throughput with zero lock contention.

---

## Why Buffering?

Standard concurrency primitives (locks, mutexes, reader-writer locks, concurrent queues) introduce thread synchronization overhead, context switches, and cache line contention.

**Buffering** replaces synchronization bottlenecks with a hardware-accelerated **Single-Writer Multiple-Reader (SWMR)** double buffering model powered by memory barriers:

- **100% Lock-Free & Non-Blocking**: Consumers read from the active front buffer without acquiring locks or waiting on the producer.
- **Decoupled Handles**: Role separation through dedicated handles (`DoubleBufferFrontReader<T>` and `DoubleBufferBackWriter<T>`).
- **Deep Memory Control**: Choose between ping-pong resource recycling (`FlipRefOrValue`) and baseline persistence (`CopyRefOrValue`).
- **Zero-Allocation Object Recycling**: Reuse expensive heap objects or unmanaged buffers across frames with zero GC pressure.
- **Smart Swap Protection**: Built-in pending update tracking guarantees that no-op swaps will never overwrite fresh front buffer state with stale data.
- **Native AOT & Trimming Ready**: Fully compatible with Native AOT compilation in .NET 8 and .NET 10+ with zero runtime reflection.

---

## Architecture & Data Flow

```
   PRODUCER (Writer Thread)                          CONSUMERS (Reader Threads)
 +--------------------------+                      +----------------------------+
 | DoubleBufferBackWriter<T>|                      | DoubleBufferFrontReader<T> |
 +--------------------------+                      +----------------------------+
              |                                                  |
              | 1. UpdateBackBuffer(data)                        | Concurrent lock-free reads
              v                                                  v
     [  Back Buffer Slot  ]                            [  Front Buffer Slot  ]
     |   Private Staging  |                            |   Active Published  |
     +--------------------+                            +---------------------+
              |                                                  ^
              | 2. SwapBuffers()                                 |
              +=================== [ Memory Barrier ] ===========+
                                  Atomic Publication
```

1. **Private Staging**: The producer writes to the isolated back buffer. The active front buffer remains unaffected.
2. **Instant Publication**: `writer.SwapBuffers()` atomically transitions the staged update into the front buffer and issues a hardware memory barrier.
3. **Contention-Free Reads**: Readers access the front buffer concurrently without ever blocking each other or the producer.

---

## Swap Effects: Complete Control Over Memory & Lifecycle

`DoubleBuffer<T>` gives you explicit control over how data transitions during a swap via `DoubleBufferSwapEffect`:

| Swap Effect | Transition | Value Types (`struct`) | Reference Types (`class`) | Best Use Case |
| :--- | :--- | :--- | :--- | :--- |
| **`FlipRefOrValue`** | `(_front, _back) = (_back, _front)` | Exchanged in-place | Pointers exchanged | **Ping-pong resource recycling**, zero-allocation buffer reuse, alternating frame buffers. |
| **`CopyRefOrValue`** | `_front = _back` | Struct value copied | Pointer copied | **Persistent baseline state**, delta updates, state accumulation across frames. |

### The Power of `FlipRefOrValue` (Zero-Allocation Recycling)
When `T` is a reference type (e.g., an array, custom buffer class, or frame payload), swapping with `FlipRefOrValue` moves the previous front buffer object into the back buffer. The writer can call `writer.ReadBackBuffer()` to retrieve and mutate that instance in-place for the next frame—**completely eliminating heap allocations and GC pauses in hot loops**.

> **⚠️ Gotcha — Mutable Object Concurrency**: Double buffering synchronizes pointer references, not internal object fields. When mutating a recycled instance in-place, ensure reader threads are not retaining or reading that same instance across frame boundaries, as concurrent mutations will cause data races. Readers should finish processing or snapshot required state during their active consumption cycle.

> **⚠️ Gotcha — Reference Aliasing (`CopyRefOrValue`)**: When `T` is a reference type, `CopyRefOrValue` copies the reference pointer, causing both front and back slots to point to the exact same heap instance. Mutating that instance in the back buffer directly modifies the active front buffer without thread isolation. Use immutable types or assign distinct instances when using copy semantics.

---

## Quick Start

Install the package and set up a lock-free double buffer in seconds:

```csharp
using Buffering.DoubleBuffering;

// Initialize double buffer with initial front/back values and a swap effect
var doubleBuffer = new DoubleBuffer<Guid>(
    Guid.Empty,
    Guid.Empty,
    DoubleBufferSwapEffect.FlipRefOrValue);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// Producer Thread: updates and publishes frames
var producerTask = Task.Run(() =>
{
    var writer = doubleBuffer.BackWriter;

    while (!cts.IsCancellationRequested)
    {
        // 1. Stage new state in the isolated back buffer
        writer.UpdateBackBuffer(Guid.NewGuid());

        // 2. Publish atomically to consumers
        writer.SwapBuffers();

        Thread.Sleep(16); // ~60 FPS update rate
    }
});

// Consumer Loop (can run on any number of concurrent threads)
var reader = doubleBuffer.FrontReader;
var lastSeen = Guid.Empty;

while (!producerTask.IsCompleted)
{
    // Read the latest published frame without locking
    var current = reader.ReadFrontBuffer();

    if (current != lastSeen)
    {
        lastSeen = current;
        Console.WriteLine($"[Frame] Received: {current}");
    }

    Thread.Sleep(8); // Consumer poll rate
}

await producerTask;
```

---

## Advanced Usage Patterns

### 1. Zero-Allocation Buffer Recycling with `FlipRefOrValue`

For high-throughput payloads (e.g., audio buffers, image frames, or byte arrays), pre-allocate two instances and ping-pong between them:

```csharp
public class FrameData
{
    public byte[] Pixels { get; } = new byte[1920 * 1080 * 4];
    public long Timestamp { get; set; }
}

// Pre-allocate both slots
var frameBuffer = new DoubleBuffer<FrameData>(
    new FrameData(),
    new FrameData(),
    DoubleBufferSwapEffect.FlipRefOrValue);

// Writer Thread
var writer = frameBuffer.BackWriter;
while (capturing)
{
    // Retrieve the recycled instance resting in the back buffer
    var target = writer.ReadBackBuffer();

    // Populate the recycled buffer directly — 0 allocations!
    CaptureCameraFrameInto(target.Pixels);
    target.Timestamp = Stopwatch.GetTimestamp();

    // Mark updated and swap into front buffer
    writer.UpdateBackBuffer(target);
    writer.SwapBuffers();
}
```

### 2. Delta & State Accumulation with `CopyRefOrValue`

When subsequent updates build upon previous state (e.g., incremental simulation ticks or cumulative metric counters), `CopyRefOrValue` keeps the back buffer intact:

```csharp
public struct WorldState
{
    public int EntityCount;
    public float SimulationTime;
}

var worldBuffer = new DoubleBuffer<WorldState>(
    default,
    default,
    DoubleBufferSwapEffect.CopyRefOrValue);

var writer = worldBuffer.BackWriter;

// Writer incrementally updates state
var state = writer.ReadBackBuffer();
state.EntityCount += 5;
state.SimulationTime += 0.016f;

writer.UpdateBackBuffer(state);
writer.SwapBuffers(); // Front receives updated copy, back preserves state for next tick
```

### 3. Fan-Out to Multiple Concurrent Readers

Any number of reader threads can read from `DoubleBufferFrontReader<T>` simultaneously without contention or performance degradation:

```csharp
var marketFeed = new DoubleBuffer<MarketSnapshot>(
    initialSnapshot,
    initialSnapshot,
    DoubleBufferSwapEffect.FlipRefOrValue);

// Spawn multiple consumer pipelines (e.g., UI, Logging, Risk Analysis)
var uiReader = Task.Run(() => RenderUI(marketFeed.FrontReader));
var riskReader = Task.Run(() => EvaluateRisk(marketFeed.FrontReader));
var telemetryReader = Task.Run(() => StreamTelemetry(marketFeed.FrontReader));
```

---

## Semantics & Concurrency Rules

| Rule | Guarantee / Requirement |
| :--- | :--- |
| **Single-Writer** | Only one thread must invoke `UpdateBackBuffer` and `SwapBuffers` at any given time on `DoubleBufferBackWriter<T>`. |
| **Multiple-Readers** | Any number of threads can safely call `ReadFrontBuffer` on `DoubleBufferFrontReader<T>` concurrently without synchronization. |
| **Memory Barriers** | Reads and swaps execute full memory barriers (`Interlocked.MemoryBarrier()`) ensuring immediate visibility across CPU caches. |
| **Swap Elision** | If `SwapBuffers()` is called without a preceding `UpdateBackBuffer()`, it returns `false` as a no-op, safeguarding fresh front state. |

---

## Potential Gotchas & Concurrency Traps

While double buffering delivers lock-free performance, improper memory handling or concurrency assumptions can lead to data races or unexpected behavior. Keep the following gotchas in mind:

### 1. Mutating Shared Reference Types During Active Read Cycles
`DoubleBuffer<T>` synchronizes **object references (pointers)**, not the internal fields of a class.
- **Problem**: When using `FlipRefOrValue` to recycle objects, the writer calls `writer.ReadBackBuffer()` after a swap to mutate the old front buffer instance in-place. If reader threads hold onto the reference returned by `reader.ReadFrontBuffer()` across frame boundaries or read asynchronously from it while the writer is mutating it, a data race will occur.
- **Solution**: Readers should finish reading data within their active consumption loop, copy/snapshot needed fields if retained, or use immutable records/objects.

### 2. Reference Aliasing with `CopyRefOrValue`
When `T` is a reference type, `CopyRefOrValue` copies the reference pointer (`_front = _back`).
- **Problem**: Following a swap, both the front and back slots point to the exact same heap instance. Mutating the object in the back buffer without creating a new instance directly alters the active front buffer, bypassing isolation.
- **Solution**: Use immutable types or assign newly instantiated/allocated objects to the back buffer when using copy semantics.

### 3. Forgetting `UpdateBackBuffer` on In-Place Mutation
`DoubleBuffer<T>` uses an internal pending update flag to prevent unintentional no-op swaps from overwriting active front state.
- **Problem**: Calling `var target = writer.ReadBackBuffer();` and mutating `target` in-place does **not** mark the buffer as pending an update. Calling `writer.SwapBuffers()` without calling `writer.UpdateBackBuffer(target)` results in a no-op that returns `false`.
- **Solution**: Always call `writer.UpdateBackBuffer(target)` after preparing or mutating back buffer data before invoking `writer.SwapBuffers()`.

### 4. Multi-Word Struct Tearing (Value Types)
In .NET / C#, copying value types larger than the native pointer width (typically 64 bits on x64 platforms) is not an atomic operation at the CPU instruction level.
- **Problem**: If `T` is a large multi-word struct (e.g., containing multiple fields), reading `_front` while a concurrent `SwapBuffers()` modifies `_front` may result in a torn read (observing a blend of old and new fields).
- **Solution**: For torn-read-free guarantees with large data structures without external locks, wrap the data in an immutable reference type / record or keep struct sizes within native register width (<= 64 bits).

### 5. Single-Writer Invariant
`DoubleBufferBackWriter<T>` is strictly single-writer (SWMR).
- **Problem**: Invoking `UpdateBackBuffer` or `SwapBuffers` concurrently from multiple producer threads without an external synchronization lock will corrupt internal state.
- **Solution**: Ensure only one thread at a time accesses `DoubleBufferBackWriter<T>`, or synchronize multiple writers with an external lock.

---

## Best Practices

- **Cache Handles Locally**: Store `FrontReader` and `BackWriter` in local variables or class fields rather than repeatedly invoking the property getter on `DoubleBuffer<T>` in hot loops.
- **Dedicated Producer Thread**: Run producer write loops on dedicated threads (or `TaskCreationOptions.LongRunning`) for deterministic pacing.
- **Always Stage In-Place Mutations**: Call `writer.UpdateBackBuffer(instance)` after modifying an object in-place from `ReadBackBuffer()` before calling `SwapBuffers()`.
- **Treat Reader References as Ephemeral**: When reading reference types, consume or snapshot the data immediately rather than storing references across frames.
- **Inspect Swap Returns**: Check the boolean return of `writer.SwapBuffers()` when conditional staging logic is used.

---

## Target Frameworks & Compatibility

- **.NET 8.0 & .NET 10.0+**
- **Native AOT & Trimming**: 100% compatible out-of-the-box.

---

## License

This project is licensed under the [MIT License](LICENSE).
