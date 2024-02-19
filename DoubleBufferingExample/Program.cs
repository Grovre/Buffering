// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering;
using Buffering.BufferResources;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;

var db = new DoubleBuffer<Vector3, Vector3>(
    rscConfiguration: new BufferResourceConfiguration<Vector3, Vector3>(
        init: (out Vector3 v3) => v3 = default,
        updater: (ref Vector3 rsc, bool _, Vector3 state) => rsc += state,
        resourceLock: new MonitorLock()),
    configuration: new DoubleBufferConfiguration(DoubleBufferSwapEffect.Flip));

using var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    var controller = db.BackController;
    while (!token.IsCancellationRequested)
    {
        controller.UpdateBackBuffer(state: Vector3.One);
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