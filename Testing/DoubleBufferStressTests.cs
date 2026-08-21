using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferStressTests
{
    [Test]
    public void HighVolume_100_000_Updates_With_8_ConcurrentReaders()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int iterations = 100_000;
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

        var readerTasks = Enumerable.Range(0, 8).Select(rId => Task.Run(() =>
        {
            int lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                int val = reader.ReadFrontBuffer();
                if (val < lastSeen)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} regression: {val} < {lastSeen}");
                }
                if (val > lastSeen)
                    lastSeen = val;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(15));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Monotonic");
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(iterations));
    }

    [Test]
    public void BurstWriter_50_Bursts_With_16_ConcurrentReaders()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        const int bursts = 50;
        const int writesPerBurst = 500;
        using var cts = new CancellationTokenSource();
        var errors = new List<string>();
        var errorLock = new object();

        var writerTask = Task.Run(() =>
        {
            int counter = 0;
            for (int b = 0; b < bursts; b++)
            {
                for (int w = 0; w < writesPerBurst; w++)
                {
                    counter++;
                    writer.UpdateBackBuffer(counter);
                    writer.SwapBuffers();
                }
                Thread.Yield();
            }
        }, cts.Token);

        var readerTasks = Enumerable.Range(0, 16).Select(rId => Task.Run(() =>
        {
            int lastSeen = 0;
            while (!cts.IsCancellationRequested)
            {
                int val = reader.ReadFrontBuffer();
                if (val < lastSeen)
                {
                    lock (errorLock)
                        errors.Add($"Reader {rId} regression during burst: {val} < {lastSeen}");
                }
                if (val > lastSeen)
                    lastSeen = val;
            }
        })).ToArray();

        writerTask.Wait();
        cts.Cancel();
        Task.WaitAll(readerTasks, TimeSpan.FromSeconds(15));

        Assert.That(errors, Is.Empty, errors.Count > 0 ? string.Join("\n", errors.Take(10)) : "Monotonic");
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(bursts * writesPerBurst));
    }

    [Test]
    public async Task AsyncConsumers_ReadingWithYieldsAndDelays_ReceivePublishedUpdates()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        using var cts = new CancellationTokenSource();
        using var ready = new CountdownEvent(2);

        var consumer1 = Task.Run(async () =>
        {
            var r = buffer.FrontReader;
            var observed = new List<int>();
            ready.Signal();
            while (!cts.IsCancellationRequested)
            {
                observed.Add(r.ReadFrontBuffer());
                await Task.Yield();
            }
            return observed;
        });

        var consumer2 = Task.Run(async () =>
        {
            var r = buffer.FrontReader;
            var observed = new List<int>();
            ready.Signal();
            while (!cts.IsCancellationRequested)
            {
                observed.Add(r.ReadFrontBuffer());
                await Task.Yield();
            }
            return observed;
        });

        Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);

        var producer = Task.Run(() =>
        {
            for (int i = 1; i <= 200; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();
                Thread.Yield();
            }
        });

        await producer;
        cts.Cancel();

        var obs1 = await consumer1;
        var obs2 = await consumer2;

        Assert.That(obs1.Count, Is.GreaterThan(0));
        Assert.That(obs2.Count, Is.GreaterThan(0));
        Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.EqualTo(200));
    }
}
