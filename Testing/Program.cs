// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering.DoubleBuffers;
using Buffering.Locking.Locks;

Console.WriteLine("Bruh");

var db = new DoubleBuffer<Vector3>(
    new BufferingResource<Vector3>(
        () => Vector3.Zero,
        (ref Vector3 rsc, bool _) => rsc = new Vector3(rsc.X + 1F)),
    new DoubleBufferConfiguration(new MonitorLock(), DoubleBufferSwapEffect.Flip));

var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    while (!token.IsCancellationRequested)
    {
        db.UpdateBackBuffer();
        db.SwapBuffers();
    }
});

while (!bufferUpdateTask.IsCompleted)
{
    db.ReadFrontBuffer(out var rsc, out var rscInfo).Dispose();
    Console.WriteLine($"{rscInfo.Id:N0}: {rsc} : {rscInfo}");
}