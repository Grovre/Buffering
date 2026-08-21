using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferConcurrencyTests
{
    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(16)]
    public void SingleWriter_MultipleReaders_StrictMonotonicity_Flip(int readerCount)
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 20_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, readerCount).Select(rId => Task.Run(() =>
        {
            int lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                int val = reader.ReadFrontBuffer();
                if (val < lastSeen)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} observed regression: {val} < {lastSeen}");
                }
                if (val > lastSeen)
                    lastSeen = val;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Monotonic");
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(iterations));
    }

    [Test]
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(8)]
    public void SingleWriter_MultipleReaders_StrictMonotonicity_Copy(int readerCount)
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.CopyRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 20_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, readerCount).Select(rId => Task.Run(() =>
        {
            int lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                int val = reader.ReadFrontBuffer();
                if (val < lastSeen)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} observed regression: {val} < {lastSeen}");
                }
                if (val > lastSeen)
                    lastSeen = val;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Monotonic");
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(iterations));
    }

    [Test]
    [TestCase(4)]
    [TestCase(8)]
    public void SingleWriter_MultipleReaders_Struct8B_Atomic_NoTornReads(int readerCount)
    {
        var buffer = new DoubleBuffer<Struct8B>(new Struct8B(0), new Struct8B(0), DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 20_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (long i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(new Struct8B(i));
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, readerCount).Select(rId => Task.Run(() =>
        {
            long lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                var val = reader.ReadFrontBuffer();
                if (val.Value < lastSeen || val.Value > iterations)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} observed invalid Struct8B value: {val.Value}");
                }
                if (val.Value > lastSeen)
                    lastSeen = val.Value;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Tear-free");
        Assert.That(reader.ReadFrontBuffer().Value, Is.EqualTo(iterations));
    }

    [Test]
    public void SingleWriter_MultipleReaders_RecordPayload_ReferenceType_NoTornReads()
    {
        var buffer = new DoubleBuffer<RecordPayload>(
            new RecordPayload(0, "0", 0.0),
            new RecordPayload(0, "0", 0.0),
            DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 15_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(new RecordPayload(i, i.ToString(), i * 1.5));
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 4).Select(rId => Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                var val = reader.ReadFrontBuffer();
                if (val == null || val.Id < 0 || val.Id > iterations || val.Text != val.Id.ToString() || !val.Ratio.Equals(val.Id * 1.5))
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} observed torn or inconsistent RecordPayload: Id={val?.Id}, Text={val?.Text}");
                }
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Tear-free");
    }

    [Test]
    public void SingleWriter_MultipleReaders_RedundantSwapsDoNotRevertPublishedState()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 15_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();

                // Intermittently perform redundant swaps
                if (i % 3 == 0)
                {
                    writer.SwapBuffers();
                    writer.SwapBuffers();
                }
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 4).Select(rId => Task.Run(() =>
        {
            int lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                int val = reader.ReadFrontBuffer();
                if (val < lastSeen)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} saw regression during redundant swaps: {val} < {lastSeen}");
                }
                if (val > lastSeen)
                    lastSeen = val;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Consistent");
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(iterations));
    }

    [Test]
    public void SingleWriter_MultipleReaders_ReferenceTypeZeroAllocationPingPong()
    {
        var obj1 = new TestObject(1, "Instance1", 0);
        var obj2 = new TestObject(2, "Instance2", 0);

        var buffer = new DoubleBuffer<TestObject>(obj1, obj2, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 10_000;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            for (int i = 1; i <= iterations; i++)
            {
                var back = writer.ReadBackBuffer();
                back.Value = i;
                writer.UpdateBackBuffer(back);
                writer.SwapBuffers();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 4).Select(rId => Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                var obj = reader.ReadFrontBuffer();
                if (obj == null)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} read null reference");
                }
                else if (obj.Value < 0 || obj.Value > iterations)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} read out-of-range value {obj.Value}");
                }
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(10));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Safe");
        Assert.That(reader.ReadFrontBuffer().Value, Is.EqualTo(iterations));
    }

    [Test]
    public void Readers_HighThroughput_NeverBlock()
    {
        var buffer = new DoubleBuffer<int>(123, 456, DoubleBufferSwapEffect.FlipRefOrValue);
        var reader = buffer.FrontReader;

        const int readsPerTask = 500_000;
        const int readerThreads = 4;

        var tasks = Enumerable.Range(0, readerThreads).Select(_ => Task.Run(() =>
        {
            int checksum = 0;
            for (int i = 0; i < readsPerTask; i++)
            {
                checksum += reader.ReadFrontBuffer();
            }
            return checksum;
        })).ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            Assert.That(task.Result, Is.Not.EqualTo(0));
        }
    }
}
