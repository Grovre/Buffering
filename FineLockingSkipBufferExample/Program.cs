// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering.BufferResources;
using Buffering.FineLockBuffering.SkipBuffering;
using Buffering.Locking.Locks;

var resources = new BufferResource<Vector3>[10];
for (var i = 0; i < resources.Length; i++)
{
    resources[i] = new BufferResource<Vector3>(
        new BufferResourceConfiguration<Vector3>(
            (out Vector3 v3) => v3 = default,
            (ref Vector3 rsc, bool _, object? state) => rsc += (Vector3)state!,
            new MonitorLock()));
}

var tf = new TaskFactory(TaskCreationOptions.LongRunning, 0);
var skipBuffer = new FineLockingSkipBuffer<Vector3>(resources);
var cts = new CancellationTokenSource(10_000);

var writerTasks = Enumerable.Range(0, 2)
    .Select(half =>
    {
        return tf.StartNew(() =>
        {
            var token = cts.Token;
            while (!token.IsCancellationRequested)
            {
                for (var i = resources.Length / 2 * half; i < resources.Length / (half == 1 ? 1 : 2); i++)
                {
                    skipBuffer.TryUpdate(i, Vector3.One);
                }
            }
        });
    }).ToArray();

var readerTasks = Enumerable.Range(0, 3)
    .Select(_ => tf.StartNew(() =>
    {
        return tf.StartNew(() =>
        {
            var token = cts.Token;
            while (!token.IsCancellationRequested)
            {
                skipBuffer.GetNext(out var rsc).Dispose();
                Console.WriteLine($"{rsc}");
            }
        });
    })).ToArray();
    
    Task.WaitAll(writerTasks);
    Task.WaitAll(readerTasks);