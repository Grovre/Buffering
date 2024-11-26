// See https://aka.ms/new-console-template for more information

using Buffering.BufferResources;
using Buffering.Locking;
using Buffering.Locking.Locks;
using Buffering.SingleBuffering;
using SingleBufferingExample;

var buffer = new SingleBuffer<BufferValues, object?>(
    new BufferResourceConfiguration<BufferValues, object?>(
        (out BufferValues rsc) => rsc = new BufferValues(0, 0F, 0L), 
        (ref BufferValues rsc, bool _, object? _) =>
        {

        }, 
        new MonitorLock()));
buffer.UpdateBuffer(new BufferValues(0, 0, 0));

using var cts = new CancellationTokenSource(10_000);

var bufferUpdaterTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    while (!token.IsCancellationRequested)
    {
        using var @lock = buffer.Lock(ResourceAccessFlags.Write);
        buffer.ReadBuffer(out var rsc, out var info);
        rsc.N += 1;
        rsc.F += 0.5F;
        rsc.L += 2L;
        buffer.UpdateBuffer(rsc);
    }
});

while (!bufferUpdaterTask.IsCompleted)
{
    Thread.Sleep(990); // Simulate work
    buffer.ReadBuffer(out var rsc, out var info).Dispose();
    Console.WriteLine($"id: {info.Id:N0}, rsc: {rsc}");
}

namespace SingleBufferingExample
{
    record struct BufferValues(int N, float F, long L);
}