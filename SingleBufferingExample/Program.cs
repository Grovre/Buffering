// See https://aka.ms/new-console-template for more information

using Buffering;
using Buffering.Locking.Locks;
using Buffering.SingleBuffering;

var buffer = new SingleBuffer<BufferValues>(
    new BufferingResource<BufferValues>(
        new BufferingResourceConfiguration<BufferValues>(
            (out BufferValues rsc) => rsc = new BufferValues(0, 0F, 0L),
            (ref BufferValues rsc, bool _) =>
            {
                rsc.N += 1;
                rsc.F += 0.5F;
                rsc.L += 2L;
            },
            new MonitorLock())));

var cts = new CancellationTokenSource(10_000);

var bufferUpdaterTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    while (!token.IsCancellationRequested)
    {
        buffer.UpdateBuffer();
    }
});

while (!bufferUpdaterTask.IsCompleted)
{
    Thread.Sleep(990); // Simulate work
    buffer.ReadBuffer(out var rsc, out var info).Dispose();
    Console.WriteLine($"id: {info.Id:N0}, rsc: {rsc}");
}

record struct BufferValues(int N, float F, long L);