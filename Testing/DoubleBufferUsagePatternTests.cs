using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferUsagePatternTests
{
    [Test]
    public void ZeroAllocationFrameRecycling_PingPongsCorrectly()
    {
        var frameA = new FrameData(64);
        var frameB = new FrameData(64);

        var buffer = new DoubleBuffer<FrameData>(frameA, frameB, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        for (int frameIdx = 1; frameIdx <= 50; frameIdx++)
        {
            var recycledFrame = writer.ReadBackBuffer();

            // Populate in place
            Array.Fill(recycledFrame.Pixels, (byte)frameIdx);
            recycledFrame.Timestamp = Stopwatch.GetTimestamp();
            recycledFrame.FrameIndex = frameIdx;

            // Mark as updated and publish
            writer.UpdateBackBuffer(recycledFrame);
            bool swapped = writer.SwapBuffers();
            Assert.That(swapped, Is.True);

            // Consumer reads
            var currentFrame = reader.ReadFrontBuffer();
            Assert.That(currentFrame.FrameIndex, Is.EqualTo(frameIdx));
            Assert.That(currentFrame.Pixels[0], Is.EqualTo((byte)frameIdx));
            Assert.That(currentFrame, Is.SameAs(recycledFrame));

            // Verify identity is strictly frameA or frameB
            Assert.That(ReferenceEquals(currentFrame, frameA) || ReferenceEquals(currentFrame, frameB), Is.True);
        }
    }

    [Test]
    public void IncrementalStateAccumulation_RetainsBaselineAcrossSwaps()
    {
        var buffer = new DoubleBuffer<WorldState>(
            default,
            default,
            DoubleBufferSwapEffect.CopyRefOrValue);

        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        for (int tick = 1; tick <= 60; tick++)
        {
            var state = writer.ReadBackBuffer();
            state.EntityCount += 5;
            state.SimulationTime += 0.016f;
            state.Tick = tick;

            writer.UpdateBackBuffer(state);
            writer.SwapBuffers();

            var published = reader.ReadFrontBuffer();
            Assert.That(published.EntityCount, Is.EqualTo(tick * 5));
            Assert.That(published.Tick, Is.EqualTo(tick));
        }
    }

    [Test]
    public void MultiConsumerTelemetryPipeline_MultipleConsumersReadIndependently()
    {
        var initial = new MarketSnapshot("MSFT", 400.0m, 400.5m, 1000, 1);
        var feed = new DoubleBuffer<MarketSnapshot>(initial, initial, DoubleBufferSwapEffect.FlipRefOrValue);

        var writer = feed.BackWriter;
        using var cts = new CancellationTokenSource();
        using var ready = new CountdownEvent(3);

        var uiReads = 0;
        var riskReads = 0;
        var telemetryReads = 0;

        var uiTask = Task.Run(() =>
        {
            var r = feed.FrontReader;
            ready.Signal();
            while (!cts.IsCancellationRequested)
            {
                var snap = r.ReadFrontBuffer();
                Assert.That(snap.Bid, Is.LessThanOrEqualTo(snap.Ask));
                Interlocked.Increment(ref uiReads);
                Thread.Yield();
            }
        });

        var riskTask = Task.Run(() =>
        {
            var r = feed.FrontReader;
            ready.Signal();
            while (!cts.IsCancellationRequested)
            {
                var snap = r.ReadFrontBuffer();
                Assert.That(snap.Volume, Is.GreaterThanOrEqualTo(1000));
                Interlocked.Increment(ref riskReads);
                Thread.Yield();
            }
        });

        var telemetryTask = Task.Run(() =>
        {
            var r = feed.FrontReader;
            ready.Signal();
            while (!cts.IsCancellationRequested)
            {
                var snap = r.ReadFrontBuffer();
                Assert.That(snap.Ticker, Is.EqualTo("MSFT"));
                Interlocked.Increment(ref telemetryReads);
                Thread.Yield();
            }
        });

        Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True, "All consumer tasks must start");

        // Producer publishes 500 updates
        for (int i = 1; i <= 500; i++)
        {
            writer.UpdateBackBuffer(new MarketSnapshot("MSFT", 400.0m + i, 400.5m + i, 1000 + i, i + 1));
            writer.SwapBuffers();
            Thread.Yield();
        }

        cts.Cancel();
        Task.WaitAll(new[] { uiTask, riskTask, telemetryTask }, TimeSpan.FromSeconds(5));

        Assert.That(uiReads, Is.GreaterThan(0));
        Assert.That(riskReads, Is.GreaterThan(0));
        Assert.That(telemetryReads, Is.GreaterThan(0));
    }

    [Test]
    public void LossyConsumer_FastProducerSlowConsumer_AlwaysGetsLatestWithoutBacklog()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        using var cts = new CancellationTokenSource();

        var producerTask = Task.Run(() =>
        {
            for (int i = 1; i <= 10_000; i++)
            {
                writer.UpdateBackBuffer(i);
                writer.SwapBuffers();
            }
        });

        var samples = new System.Collections.Generic.List<int>();
        while (!producerTask.IsCompleted)
        {
            samples.Add(reader.ReadFrontBuffer());
            Thread.Sleep(1);
        }

        producerTask.Wait();
        var finalValue = reader.ReadFrontBuffer();

        Assert.That(finalValue, Is.EqualTo(10_000));
        Assert.That(samples.Count, Is.GreaterThan(0));
    }

    [Test]
    public void DisposablePayloadManagement_CallerRetainsOwnershipAndCanDispose()
    {
        var payload1 = new DisposablePayload(1);
        var payload2 = new DisposablePayload(2);

        var buffer = new DoubleBuffer<DisposablePayload>(payload1, payload2, DoubleBufferSwapEffect.FlipRefOrValue);

        Assert.That(payload1.IsDisposed, Is.False);
        Assert.That(payload2.IsDisposed, Is.False);

        buffer.SwapBuffers();

        // Buffer operations do not automatically dispose
        Assert.That(payload1.IsDisposed, Is.False);
        Assert.That(payload2.IsDisposed, Is.False);

        // Caller can safely retrieve and dispose references
        var front = buffer.FrontReader.ReadFrontBuffer();
        var back = buffer.BackWriter.ReadBackBuffer();

        front.Dispose();
        back.Dispose();

        Assert.That(payload1.IsDisposed, Is.True);
        Assert.That(payload2.IsDisposed, Is.True);
    }
}
