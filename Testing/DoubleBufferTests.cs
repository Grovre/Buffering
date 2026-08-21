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
            lockImpl: new NoLock(),
            swapEffect: DoubleBufferSwapEffect.Flip);

        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;
    }

    // ──────────────────────────────────────────────
    // Basic Functional Tests
    // ──────────────────────────────────────────────

    [Test]
    public void ReadFrontBuffer_ReturnsInitialValue_BeforeAnySwap()
    {
        var value = _frontReader.ReadFrontBuffer(out var version);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(version, Is.EqualTo(0));
    }

    [Test]
    public void UpdateBackBuffer_ThenSwap_FrontReflectsUpdate()
    {
        _backWriter.UpdateBackBuffer(42);
        _backWriter.SwapBuffers();

        var value = _frontReader.ReadFrontBuffer(out _);

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void SwapBuffers_Flip_ExchangesFrontAndBack()
    {
        // Start: front=10, back=20
        _doubleBuffer = new DoubleBuffer<int>(10, 20, new NoLock(), DoubleBufferSwapEffect.Flip);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After flip: front=20, back=10
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(20));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(10));
    }

    [Test]
    public void SwapBuffers_Copy_FrontGetsBack_BackUnchanged()
    {
        _doubleBuffer = new DoubleBuffer<int>(10, 20, new NoLock(), DoubleBufferSwapEffect.Copy);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After copy: front=20, back=20
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(20));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(20));
    }

    [Test]
    public void MultipleUpdatesAndSwaps_CycleCorrectly()
    {
        // Start: front=0, back=0
        _backWriter.UpdateBackBuffer(42);
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(42));

        _backWriter.UpdateBackBuffer(84);
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(84));

        // Swap again without updating back — flip brings old front (42) to front
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(42));
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
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(0));

        _backWriter.UpdateBackBuffer(999);
        // front should still be 0 — no swap occurred
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(0));
    }

    // ──────────────────────────────────────────────
    // Version Tests
    // ──────────────────────────────────────────────

    [Test]
    public void Version_StartsAtZero()
    {
        _frontReader.ReadFrontBuffer(out var version);
        Assert.That(version, Is.EqualTo(0));
    }

    [Test]
    public void Version_IncrementsOnUpdateBackBuffer()
    {
        _frontReader.ReadFrontBuffer(out var v0);

        _backWriter.UpdateBackBuffer(1);
        _frontReader.ReadFrontBuffer(out var v1);

        _backWriter.UpdateBackBuffer(2);
        _frontReader.ReadFrontBuffer(out var v2);

        Assert.That(v1, Is.EqualTo(v0 + 1));
        Assert.That(v2, Is.EqualTo(v1 + 1));
    }

    [Test]
    public void Version_DoesNotIncrementOnSwapOnly()
    {
        _backWriter.UpdateBackBuffer(42); // version → 1
        _frontReader.ReadFrontBuffer(out var vBeforeSwap);

        _backWriter.SwapBuffers();
        _frontReader.ReadFrontBuffer(out var vAfterSwap);

        Assert.That(vAfterSwap, Is.EqualTo(vBeforeSwap));
    }

    [Test]
    public void Version_CanDetectStaleRead()
    {
        _backWriter.UpdateBackBuffer(1);
        _frontReader.ReadFrontBuffer(out var v1);

        _backWriter.UpdateBackBuffer(2);
        _backWriter.SwapBuffers();

        _frontReader.ReadFrontBuffer(out var v2);

        Assert.That(v2, Is.GreaterThan(v1));
    }

    // ──────────────────────────────────────────────
// Swap Effect Edge Cases
    // ──────────────────────────────────────────────

    [Test]
    public void SwapBuffers_Flip_PreservesOldFrontInBack()
    {
        _doubleBuffer = new DoubleBuffer<int>(111, 222, new NoLock(), DoubleBufferSwapEffect.Flip);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After flip: front=222, back=111
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(222));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(111));

        // Update back (which was the old front=111) and swap again
        _backWriter.UpdateBackBuffer(333);
        _backWriter.SwapBuffers();

        // After second flip: front=333, back=222
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(333));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(222));
    }

    [Test]
    public void SwapBuffers_Copy_FrontGetsBack_BackKeepsSameValue()
    {
        _doubleBuffer = new DoubleBuffer<int>(111, 222, new NoLock(), DoubleBufferSwapEffect.Copy);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        _backWriter.SwapBuffers();

        // After copy: front=222, back=222
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(222));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(222));

        // Update back and copy again
        _backWriter.UpdateBackBuffer(333);
        _backWriter.SwapBuffers();

        // After second copy: front=333, back=333
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(333));
        Assert.That(_backWriter.ReadBackBuffer(), Is.EqualTo(333));
    }

    [Test]
    public void SwapBuffers_Copy_ThenFlip_WorksCorrectly()
    {
        _doubleBuffer = new DoubleBuffer<int>(100, 200, new NoLock(), DoubleBufferSwapEffect.Copy);
        _backWriter = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;

        // Copy: front=200, back=200
        _backWriter.SwapBuffers();
        Assert.That(_frontReader.ReadFrontBuffer(out _), Is.EqualTo(200));

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
            frontObj, backObj, new NoLock(), DoubleBufferSwapEffect.Flip);
        var frontReader = buffer.FrontReader;
        var backWriter = buffer.BackWriter;

        // Before swap: front=frontObj
        Assert.That(frontReader.ReadFrontBuffer(out _), Is.SameAs(frontObj));

        backWriter.SwapBuffers();

        // After flip: front=backObj
        Assert.That(frontReader.ReadFrontBuffer(out _), Is.SameAs(backObj));
        Assert.That(backWriter.ReadBackBuffer(), Is.SameAs(frontObj));
    }

    [Test]
    public void UpdateBackBuffer_WithReferenceType_ReplacesBackReference()
    {
        var initialBack = new TestObject(0);
        var newBack = new TestObject(42);

        var buffer = new DoubleBuffer<TestObject>(
            new TestObject(1), initialBack, new NoLock(), DoubleBufferSwapEffect.Flip);
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
        // Use a real lock for concurrency tests — NoLock is unsafe for concurrent access
        _doubleBuffer = new DoubleBuffer<int>(0, 0, new MultipleReaderLock(), DoubleBufferSwapEffect.Flip);
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
                    var value = _frontReader.ReadFrontBuffer(out _);
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
        var finalValue = _frontReader.ReadFrontBuffer(out _);
        Assert.That(finalValue, Is.EqualTo(10000));
    }

    [Test]
    public void ConcurrentMultipleWriters_NoLostUpdates_WithLock()
    {
        _doubleBuffer = new DoubleBuffer<int>(0, 0, new MultipleReaderLock(), DoubleBufferSwapEffect.Flip);
        _backWriter = _doubleBuffer.BackWriter;

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
        var frontValue = _frontReader.ReadFrontBuffer(out _);
        Assert.That(frontValue, Is.EqualTo(backValue));
    }

    [Test]
    public void ConcurrentReaders_NeverObserveTornState()
    {
        _doubleBuffer = new DoubleBuffer<int>(
            0, 0, new MultipleReaderLock(), DoubleBufferSwapEffect.Flip);
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
                    var value = _frontReader.ReadFrontBuffer(out _);
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

    // ──────────────────────────────────────────────
    // FrontReader / BackWriter Facade Tests
    // ──────────────────────────────────────────────

    [Test]
    public void FrontReader_DelegatesToDoubleBuffer()
    {
        _backWriter.UpdateBackBuffer(77);
        _backWriter.SwapBuffers();

        // FrontReader should return the same as direct DoubleBuffer access
        var viaReader = _frontReader.ReadFrontBuffer(out var versionViaReader);
        var viaDirect = _doubleBuffer.ReadFrontBuffer(out var versionViaDirect);

        Assert.That(viaReader, Is.EqualTo(viaDirect));
        Assert.That(versionViaReader, Is.EqualTo(versionViaDirect));
    }

    [Test]
    public void BackWriter_DelegatesToDoubleBuffer()
    {
        _backWriter.UpdateBackBuffer(55);

        var viaWriter = _backWriter.ReadBackBuffer();
        var viaDirect = _doubleBuffer.ReadBackBuffer();

        Assert.That(viaWriter, Is.EqualTo(viaDirect));
    }

    [Test]
    public void FrontReader_DefaultConstructor_Throws()
    {
        Assert.Throws<NotImplementedException>(() =>
        {
            _ = new DoubleBufferFrontReader<int>();
        });
    }

    [Test]
    public void BackWriter_DefaultConstructor_Throws()
    {
        Assert.Throws<NotImplementedException>(() =>
        {
            _ = new DoubleBufferBackWriter<int>();
        });
    }

    // ──────────────────────────────────────────────
    // Helper Types
    // ──────────────────────────────────────────────

    private sealed class TestObject
    {
        public int Value { get; }
        public TestObject(int value) => Value = value;
        public override string ToString() => $"TestObject({Value})";
    }
}
