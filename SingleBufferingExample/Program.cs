// See https://aka.ms/new-console-template for more information

using Buffering;
using Buffering.Locking.Locks;
using Buffering.SingleBuffering;

var buffer = new SingleBuffer<BufferValues>(
    new BufferingResource<BufferValues>(
        () => default,
        (ref BufferValues values, bool _) =>
        {
            values.N += 1;
            values.F += 0.5f;
            values.L += 2L;
        }),
    new SingleBufferConfiguration(new MonitorLock()));

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