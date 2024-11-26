// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Numerics;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;

var db = new DoubleBuffer<Vector3>(
    new MonitorLock(),
    DoubleBufferSwapEffect.Flip);

using var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    var controller = db.BackWriter;
    var start = Stopwatch.GetTimestamp();
    while (!token.IsCancellationRequested)
    {
        var elapsed = Stopwatch.GetElapsedTime(start);
        var scaledUp = (int)(Math.Cos(elapsed.Seconds / (Math.PI * 2)) * 100);
        var v3 = new Vector3(scaledUp);
        controller.UpdateBackBuffer(v3);
        controller.SwapBuffers();
    }
});

var reader = db.FrontReader;
while (!bufferUpdateTask.IsCompleted)
{
    Thread.Sleep(990); // Simulate work
    reader.ReadFrontBuffer(out var rsc, out var rscInfo).Dispose();
    Console.WriteLine($"{rscInfo.Id:N0}: {rsc} : {rscInfo}");
}