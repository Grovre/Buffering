// See https://aka.ms/new-console-template for more information

using Buffering.DoubleBuffering;

var db = new DoubleBuffer<Guid>(
    Guid.Empty,
    Guid.Empty,
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
var lastGuid = Guid.Empty;
while (!bufferUpdateTask.IsCompleted)
{
    Thread.Sleep(100); // Simulate work
    
    var guid = reader.ReadFrontBuffer();
    
    if (lastGuid == guid)
        continue;
    
    lastGuid = guid;
    Console.WriteLine(guid);
}