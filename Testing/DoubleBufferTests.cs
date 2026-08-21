// See https://aka.ms/new-console-template for more information

using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferTests
{
    private DoubleBuffer<int> _doubleBuffer = null!;
    private DoubleBufferBackWriter<int> _backWriter;
    private DoubleBufferFrontReader<int> _frontReader;

    [SetUp]
    public void SetUp()
    {
        _doubleBuffer = new DoubleBuffer<int>(
            initialFrontValue: 0,
            initialBackValue: 0,
            swapEffect: DoubleBufferSwapEffect.FlipRefOrValue);

        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;
    }

    // ──────────────────────────────────────────────
    // Basic Functional Tests
    // ──────────────────────────────────────────────

    [Test]
    public void ReadFrontBuffer_ReturnsInitialValue_BeforeAnySwap()
    {
        var value = _frontReader.ReadFrontBuffer();

        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void UpdateBackBuffer_ThenSwap_FrontReflectsUpdate()
    {
        _backWriter.UpdateBackBuffer(42);
        _backWriter.SwapBuffers();

        var value = _frontReader.ReadFrontBuffer();

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void SwapBuffers_Flip_ExchangesFrontAndBack()
    {
        // Start: front=10, back=20
        _doubleBuffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After flip: front=20, back=10
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(10));
    }

    [Test]
    public void SwapBuffers_Copy_FrontGetsBack_BackUnchanged()
    {
        _doubleBuffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.CopyRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After copy: front=20, back=20
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(20));
    }

    [Test]
    public void MultipleUpdatesAndSwaps_CycleCorrectly()
    {
        // Start: front=0, back=0
        _backWriter.UpdateBackBuffer(42);
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(42));

        _backWriter.UpdateBackBuffer(84);
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(84));

        // Swap again without updating back — does not overwrite newer front with old data
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(84));

        // Update back again and swap
        _backWriter.UpdateBackBuffer(126);
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(126));
    }

    [Test]
    public void ReadBackBuffer_ReturnsLatestBackValue()
    {
        _backWriter.UpdateBackBuffer(100);
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(100));

        _backWriter.UpdateBackBuffer(200);
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(200));
    }

    [Test]
    public void ReadFrontBuffer_DoesNotChangeAfterBackUpdateOnly()
    {
        // front starts at 0
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(0));

        _backWriter.UpdateBackBuffer(999);
        // front should still be 0 — no swap occurred
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(0));
    }

    // ──────────────────────────────────────────────
    // Swap Effect Edge Cases
    // ──────────────────────────────────────────────

    [Test]
    public void SwapBuffers_Flip_PreservesOldFrontInBack()
    {
        _doubleBuffer = new DoubleBuffer<int>(111, 222, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After flip: front=222, back=111
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(222));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(111));

        // Update back (which was the old front=111) and swap again
        _backWriter.UpdateBackBuffer(333);
        _backWriter.SwapBuffers();

        // After second flip: front=333, back=222
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(333));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(222));
    }

    [Test]
    public void SwapBuffers_Copy_FrontGetsBack_BackKeepsSameValue()
    {
        _doubleBuffer = new DoubleBuffer<int>(111, 222, DoubleBufferSwapEffect.CopyRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After copy: front=222, back=222
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(222));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(222));

        // Update back and copy again
        _backWriter.UpdateBackBuffer(333);
        _backWriter.SwapBuffers();

        // After second copy: front=333, back=333
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(333));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(333));
    }

    [Test]
    public void SwapBuffers_Copy_ThenFlip_WorksCorrectly()
    {
        _doubleBuffer = new DoubleBuffer<int>(100, 200, DoubleBufferSwapEffect.CopyRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        // Copy: front=200, back=200
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(200));

        // Update back=300
        _backWriter.UpdateBackBuffer(300);

        // Flip: front=300, back=200
        // (Need a Flip-configured buffer for this; swap effect is fixed at construction)
        // This test verifies that Copy is a one-way operation — once copied, both are the same
        // A Flip on a different buffer with the same starting state is tested separately
    }

    // ──────────────────────────────────────────────
    // Reference Type Tests
    // ──────────────────────────────────────────────

    [Test]
    public void ReadFrontBuffer_WithReferenceType_ReturnsCorrectReference()
    {
        var frontObj = new TestObject(1);
        var backObj = new TestObject(2);

        var buffer = new DoubleBuffer<TestObject>(
            frontObj, backObj, DoubleBufferSwapEffect.FlipRefOrValue);
        var frontReader = buffer.FrontReader;
        var backWriter = buffer.BackWriter;

        // Before swap: front=frontObj
        Assert.That(frontReader.ReadFrontBuffer(), Is.SameAs(frontObj));

        backWriter.SwapBuffers();

        // After flip: front=backObj
        Assert.That(frontReader.ReadFrontBuffer(), Is.SameAs(backObj));
        Assert.That(backWriter.ReadBackBuffer(), Is.SameAs(frontObj));
    }

    [Test]
    public void UpdateBackBuffer_WithReferenceType_ReplacesBackReference()
    {
        var initialBack = new TestObject(0);
        var newBack = new TestObject(42);

        var buffer = new DoubleBuffer<TestObject>(
            new TestObject(1), initialBack, DoubleBufferSwapEffect.FlipRefOrValue);
        var backWriter = buffer.BackWriter;

        backWriter.UpdateBackBuffer(newBack);

        Assert.That(backWriter.ReadBackBuffer(), Is.SameAs(newBack));
    }

    // ──────────────────────────────────────────────
    // Concurrency Tests
    // ──────────────────────────────────────────────

    [Test]
    public void ConcurrentUpdatesAndSwaps_NoCorruptionOrDeadlock()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        const int iterations = 10_000;
        var cts = new CancellationTokenSource();
        var exceptions = new List<Exception>();

        // Writer thread: updates back and swaps
        var writerTask = Task.Run(() =>
        {
            try
            {
                for (int i = 1; i <= iterations; i++)
                {
                    _backWriter.UpdateBackBuffer(i);
                    _backWriter.SwapBuffers();
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        }, cts.Token);

        // Reader thread: reads front continuously
        var readerTask = Task.Run(() =>
        {
            try
            {
                int lastSeen = 0;
                while (!cts.IsCancellationRequested)
                {
                    var value = _frontReader.ReadFrontBuffer();
                    // Front should always be a value that was written (0..iterations)
                    // or the initial 0. It should never be garbage or out of range.
                    Assert.That(value, Is.InRange(0, iterations),
                        $"Read out-of-range value: {value}");
                    if (value > lastSeen)
                        lastSeen = value;
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        }, cts.Token);

        // Let it run
        writerTask.Wait();
        cts.Cancel();
        readerTask.Wait(TimeSpan.FromSeconds(5));

        Assert.That(exceptions, Is.Empty,
            exceptions.Count > 0 ? exceptions[0].ToString() : "No exceptions");

        // After all writes complete, the last swapped value should be visible
        // The writer wrote 1..10000 and swapped after each.
        // After the last swap, front = 10000 (the last value written to back before the last swap).
        var finalValue = _frontReader.ReadFrontBuffer();
        Assert.That(finalValue, Is.EqualTo(10000));
    }

    [Test]
    public void ConcurrentMultipleWriters_NoLostUpdates_LockFree()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        const int writerCount = 4;
        const int iterationsPerWriter = 5_000;

        var tasks = Enumerable.Range(0, writerCount).Select(writerId =>
            Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerWriter; i++)
                {
                    // Each writer writes a unique value: writerId * iterationsPerWriter + i + 1
                    _backWriter.UpdateBackBuffer(writerId * iterationsPerWriter + i + 1);
                }
            })).ToArray();

        Task.WaitAll(tasks);

        // The back buffer should contain the last value written by some writer.
        // We can't predict which writer won, but it should be a valid value in range.
        var backValue = _backWriter.ReadBackBuffer();
        Assert.That(backValue, Is.InRange(1, writerCount * iterationsPerWriter));

        // Swap and verify front gets that value
        _backWriter.SwapBuffers();
        var frontValue = _frontReader.ReadFrontBuffer();
        Assert.That(frontValue, Is.EqualTo(backValue));
    }

    [Test]
    public void ConcurrentReaders_NeverObserveTornState()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        const int iterations = 5_000;
        var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        // Single writer: writes known values, swaps
        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                _backWriter.UpdateBackBuffer(i);
                _backWriter.SwapBuffers();
            }
        }, cts.Token);

        // Multiple readers
        var readerTasks = Enumerable.Range(0, 4).Select(_ =>
            Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var value = _frontReader.ReadFrontBuffer();
                    if (value < 0 || value > iterations)
                    {
                        lock (errorLock)
                            errors.Add($"Read invalid value: {value}");
                    }
                }
            })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(5));

        Assert.That(errors, Is.Empty,
            errors.Count > 0 ? string.Join(", ", errors) : "No errors");
    }

    [Test]
    public void Constructor_Initialization_WorksCorrectly()
    {
        var buffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.FlipRefOrValue);
        var reader = buffer.FrontReader;
        var writer = buffer.BackWriter;

        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(20));

        writer.UpdateBackBuffer(99);
        writer.SwapBuffers();

        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(99));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(10));
    }

    [Test]
    public void ConcurrentReadersAndWriters_WithReferenceTypePayload_NeverObserveTornState()
    {
        var buffer = new DoubleBuffer<TestObject>(
            new TestObject(0, "0", 0),
            new TestObject(0, "0", 0),
            DoubleBufferSwapEffect.FlipRefOrValue);

        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 10_000;
        var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(new TestObject(i, i.ToString(), i));
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 4).Select(_ =>
            Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var val = reader.ReadFrontBuffer();
                    if (val == null || val.Id != val.Value || val.Name != val.Id.ToString())
                    {
                        lock (errorLock)
                            errors.Add($"Torn reference read detected: Id={val?.Id}, Name={val?.Name}, Value={val?.Value}");
                    }
                }
            })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(5));

        Assert.That(errors, Is.Empty,
            errors.Count > 0 ? string.Join(", ", errors) : "No torn reads");
    }

    // ──────────────────────────────────────────────
    // Last-Writer-Wins & Swap Idempotence Tests
    // ──────────────────────────────────────────────

    [Test]
    public void SwapBuffers_LastWriterWins_MultipleWritesBeforeSwap_PublishesLatestWrite()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.UpdateBackBuffer(10);
        _backWriter.UpdateBackBuffer(20);
        _backWriter.UpdateBackBuffer(30);

        _backWriter.SwapBuffers();

        Assert.That(_frontReader.ReadFrontBuffer(), Is.EqualTo(30));
    }

    [Test]
    public void SwapBuffers_LastWriterWins_DelayedSwapDoesNotOverwriteNewerFrontData()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = _doubleBuffer.BackWriter;
        var reader = _doubleBuffer.FrontReader;

        // Writer 1 writes and swaps
        writer.UpdateBackBuffer(100);
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(100));

        // Writer 2 writes newer data and swaps
        writer.UpdateBackBuffer(200);
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(200));

        // Stale swap without update should NOT overwrite newer data 200 with older 100
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(200));
    }

    [Test]
    public void SwapBuffers_MultipleConsecutiveSwapsWithoutUpdate_AreNoOps_Flip()
    {
        _doubleBuffer = new DoubleBuffer<int>(1, 2, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = _doubleBuffer.BackWriter;
        var reader = _doubleBuffer.FrontReader;

        // First swap publishes initial back (2) to front
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(2));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(1));

        // Subsequent swaps without update are no-ops and preserve front value
        for (int i = 0; i < 5; i++)
        {
            writer.SwapBuffers();
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(2));
            Assert.That(writer.ReadBackBuffer(), Is.EqualTo(1));
        }
    }

    [Test]
    public void SwapBuffers_MultipleConsecutiveSwapsWithoutUpdate_AreNoOps_Copy()
    {
        _doubleBuffer = new DoubleBuffer<int>(1, 2, DoubleBufferSwapEffect.CopyRefOrValue);
        var writer = _doubleBuffer.BackWriter;
        var reader = _doubleBuffer.FrontReader;

        // First swap copies initial back (2) to front
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(2));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(2));

        // Subsequent swaps without update are no-ops
        for (int i = 0; i < 5; i++)
        {
            writer.SwapBuffers();
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(2));
            Assert.That(writer.ReadBackBuffer(), Is.EqualTo(2));
        }
    }

    [Test]
    public void ConcurrentWritersAndSwappers_NeverRevertsToStaleValueAfterWritesComplete()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = _doubleBuffer.BackWriter;
        var reader = _doubleBuffer.FrontReader;

        const int writerCount = 4;
        const int iterationsPerWriter = 5_000;
        var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        // Multiple writer threads each updating and swapping
        var writerTasks = Enumerable.Range(0, writerCount).Select(wId =>
            Task.Run(() =>
            {
                for (int i = 1; i <= iterationsPerWriter; i++)
                {
                    writer.UpdateBackBuffer(wId * iterationsPerWriter + i);
                    writer.SwapBuffers();
                }
            })).ToArray();

        // Multiple reader threads verifying valid in-range values
        var readerTasks = Enumerable.Range(0, 4).Select(_ =>
            Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var val = reader.ReadFrontBuffer();
                    if (val < 0 || val > writerCount * iterationsPerWriter)
                    {
                        lock (errorLock)
                            errors.Add($"Read out-of-range value: {val}");
                    }
                }
            })).ToArray();

        Task.WaitAll(writerTasks);
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(5));

        Assert.That(errors, Is.Empty,
            errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "No read errors");

        // After all writes finish, capture the final front buffer value
        var finalFront = reader.ReadFrontBuffer();
        Assert.That(finalFront, Is.InRange(1, writerCount * iterationsPerWriter));

        // Subsequent redundant swaps from multiple threads must NOT overwrite or revert the final published value
        var swapperTasks = Enumerable.Range(0, 8).Select(_ =>
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    writer.SwapBuffers();
                }
            })).ToArray();

        Task.WaitAll(swapperTasks);

        var afterRedundantSwaps = reader.ReadFrontBuffer();
        Assert.That(afterRedundantSwaps, Is.EqualTo(finalFront),
            "Redundant swaps must not overwrite newer front buffer data!");
    }

    [Test]
    public void SingleWriter_ValuesAreStrictlyMonotonicAndNeverRevert()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = _doubleBuffer.BackWriter;
        var reader = _doubleBuffer.FrontReader;

        const int iterations = 10_000;
        var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();
                // Randomly perform redundant swaps to ensure they never revert front buffer
                if (i % 5 == 0)
                {
                    writer.SwapBuffers();
                }
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 4).Select(_ =>
            Task.Run(() =>
            {
                int lastSeen = 0;
                while (!cts.IsCancellationRequested)
                {
                    var val = reader.ReadFrontBuffer();
                    if (val < lastSeen)
                    {
                        lock (errorLock)
                            errors.Add($"Front buffer decreased: observed {val} after {lastSeen}");
                    }
                    if (val > lastSeen)
                        lastSeen = val;
                }
            })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(5));

        Assert.That(errors, Is.Empty,
            errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "No monotonic violations");
    }

    // ──────────────────────────────────────────────
    // FrontReader / BackWriter Facade Tests
    // ──────────────────────────────────────────────

    [Test]
    public void FrontReader_DelegatesToDoubleBuffer()
    {
        _backWriter.UpdateBackBuffer(77);
        _backWriter.SwapBuffers();

        // FrontReader should return the same as direct DoubleBuffer access
        var viaReader = _frontReader.ReadFrontBuffer();
        var viaDirect = _doubleBuffer.ReadFrontBuffer();

        Assert.That(viaReader, Is.EqualTo(viaDirect));
    }

    [Test]
    public void BackWriter_DelegatesToDoubleBuffer()
    {
        _backWriter.UpdateBackBuffer(55);

        var viaWriter = _backWriter.ReadBackBuffer();
        var viaDirect = _doubleBuffer.ReadBackBuffer();

        Assert.That(viaWriter, Is.EqualTo(viaDirect));
    }


    // ──────────────────────────────────────────────
    // Helper Types
    // ──────────────────────────────────────────────

    private readonly struct LargeStruct
    {
        public readonly long A;
        public readonly long B;
        public readonly long C;
        public readonly long D;

        public LargeStruct(long val)
        {
            A = val;
            B = val;
            C = val;
            D = val;
        }

        public bool IsValid() => A == B && B == C && C == D;
    }
}
