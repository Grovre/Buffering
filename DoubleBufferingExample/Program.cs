// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Numerics;
using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;

var db = new DoubleBuffer<Guid>(
    Guid.Empty,
    Guid.Empty,
    new SystemThreadingLock(),
    DoubleBufferSwapEffect.Flip);

using var cts = new CancellationTokenSource(10_000);

var bufferUpdateTask = Task.Run(() =>
{
    try
    {
        var ct = cts.Token;
        var writer = db.BackWriter;
        while (!ct.IsCancellationRequested)
        {
            //var elapsed = Stopwatch.GetElapsedTime(start);
            Thread.Sleep(1000);
            writer.UpdateBackBuffer(Guid.CreateVersion7());
            writer.SwapBuffers();
        }
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        // This is fine
    }
}, cts.Token);

var reader = db.FrontReader;
var lastVersion = 0;
while (!bufferUpdateTask.IsCompleted)
{
    Thread.Sleep(100); // Simulate work
    
    var guid = reader.ReadFrontBuffer(out var version);
    
    if (lastVersion == version)
        continue;
    
    lastVersion = version;
    Console.WriteLine(guid);
}