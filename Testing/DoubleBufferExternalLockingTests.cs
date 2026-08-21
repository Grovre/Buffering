using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buffering.DoubleBuffering;
using Buffering.Locking;
using Buffering.Locking.Locks;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferExternalLockingTests
{
    [Test]
    public void MultipleWriters_WithStandardLock_MaintainsStrictConsistency()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int writersCount = 4;
        const int iterationsPerWriter = 2_500;
        var writeLock = new object();
        var writtenValues = new HashSet<int>();

        var writerTasks = Enumerable.Range(0, writersCount).Select(wId => Task.Run(() =>
        {
            for (int i = 1; i <= iterationsPerWriter; i++)
            {
                int value = wId * iterationsPerWriter + i;
                lock (writeLock)
                {
                    writer.UpdateBackBuffer(value);
                    writer.SwapBuffers();
                    writtenValues.Add(value);
                }
            }
        })).ToArray();

        var readerTasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 5_000; i++)
            {
                int val = reader.ReadFrontBuffer();
                Assert.That(val, Is.GreaterThanOrEqualTo(0));
            }
        })).ToArray();

        Task.WaitAll(writerTasks);
        Task.WaitAll(readerTasks);

        var finalFront = reader.ReadFrontBuffer();
        Assert.That(writtenValues.Contains(finalFront), Is.True);
        Assert.That(writtenValues.Count, Is.EqualTo(writersCount * iterationsPerWriter));
    }

    [Test]
    public void MultipleWriters_WithMonitorLock_MaintainsStrictConsistency()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;
        var monitorLock = new MonitorLock();

        const int writersCount = 4;
        const int iterationsPerWriter = 2_000;

        var writerTasks = Enumerable.Range(0, writersCount).Select(wId => Task.Run(() =>
        {
            for (int i = 1; i <= iterationsPerWriter; i++)
            {
                int value = wId * iterationsPerWriter + i;
                using (monitorLock.Lock(ResourceAccessFlags.Write))
                {
                    writer.UpdateBackBuffer(value);
                    writer.SwapBuffers();
                }
            }
        })).ToArray();

        Task.WaitAll(writerTasks);

        var finalFront = reader.ReadFrontBuffer();
        Assert.That(finalFront, Is.InRange(1, writersCount * iterationsPerWriter));
    }

    [Test]
    public void MultipleWriters_WithSpinnerLock_MaintainsStrictConsistency()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;
        var spinnerLock = new SpinnerLock();

        const int writersCount = 4;
        const int iterationsPerWriter = 2_000;

        var writerTasks = Enumerable.Range(0, writersCount).Select(wId => Task.Run(() =>
        {
            for (int i = 1; i <= iterationsPerWriter; i++)
            {
                int value = wId * iterationsPerWriter + i;
                using (spinnerLock.Lock(ResourceAccessFlags.Write))
                {
                    writer.UpdateBackBuffer(value);
                    writer.SwapBuffers();
                }
            }
        })).ToArray();

        Task.WaitAll(writerTasks);

        var finalFront = reader.ReadFrontBuffer();
        Assert.That(finalFront, Is.InRange(1, writersCount * iterationsPerWriter));
    }
}
