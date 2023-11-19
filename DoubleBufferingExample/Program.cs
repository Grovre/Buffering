// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;

var db = new DoubleBuffer<Vector3>(
    rsc: new BufferingResource<Vector3>(
        init: () => Vector3.Zero,
        updater: (ref Vector3 rsc, bool _) => rsc = new Vector3(rsc.X + 1F),
        new MonitorLock()),
    configuration: new DoubleBufferConfiguration(
        swapEffect: DoubleBufferSwapEffect.Flip));

var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    var controller = db.BackController;
    while (!token.IsCancellationRequested)
    {
        controller.UpdateBackBuffer();
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