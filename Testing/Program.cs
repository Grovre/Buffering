// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering.DoubleBuffers;
using Buffering.Locking.Locks;

Console.WriteLine("Bruh");

var i = 0;
var db = new DoubleBuffer<Vector3>(
    new BufferingResource<Vector3>(
        Vector3.Zero,
        (ref Vector3 rsc) => rsc = new Vector3(i)),
    new DoubleBufferConfiguration(new MonitorLock()));
var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    while (!token.IsCancellationRequested)
    {
        db.UpdateBackBuffer();
        db.SwapBuffers();
        i++;
    }
});

while (!bufferUpdateTask.IsCompleted)
{
    db.ReadFrontBuffer(out var v3, out var rscInfo).Dispose();
    Console.WriteLine($"{i:N0}: {v3} : {rscInfo}");
}