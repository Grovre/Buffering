// See https://aka.ms/new-console-template for more information

using System.Numerics;
using System.Text;
using System.Threading.Channels;
using Buffering.DoubleBuffers;

Console.WriteLine("Bruh");

var i = 0;
var j = 0;
var db = new DoubleBuffer<Vector3>(new BufferingResource<Vector3>(Vector3.Zero, (ref Vector3 rsc) => rsc += Vector3.One));
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
    db.ReadFrontBuffer(out var v3).Dispose();
    Console.WriteLine($"{i++} ({j}): {v3}");
}