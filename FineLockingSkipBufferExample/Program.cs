// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering.BufferResources;
using Buffering.FineLockBuffering.SkipBuffering;
using Buffering.Locking.Locks;

var resources = new BufferResource<Vector3, Vector3>[10];
for (var i = 0; i < resources.Length; i++)
{
    resources[i] = new BufferResource<Vector3, Vector3>(
        new BufferResourceConfiguration<Vector3, Vector3>(
            (out Vector3 v3) => v3 = default,
            (ref Vector3 rsc, bool _, Vector3 state) => rsc += state,
            new MonitorLock()));
}

var tf = new TaskFactory(TaskCreationOptions.LongRunning, 0);
var skipBuffer = new FineLockingSkipBuffer<Vector3, Vector3>(resources);

var cts = new CancellationTokenSource(10_000);

var writerTasks = Enumerable.Range(0, resources.Length)
    .Select(i =>
    {
        return tf.StartNew(() =>
        {
            var token = cts.Token;
            var updater = skipBuffer.Updater;
            var j = i;
            while (!token.IsCancellationRequested)
            {
                updater.TryUpdate(j, Vector3.One);
            }
        });
    }).ToArray();

while (Array.Exists(writerTasks, t => !t.IsCompleted))
{
    Thread.Sleep(990);
    skipBuffer.ReadNextUnlockedBuffer(out var rsc, out var info).Dispose();
    Console.WriteLine($"{info}: {rsc}");
}

var sum = 0;
for (var i = 0; i < resources.Length; i++)
{
    skipBuffer.ReadNextUnlockedBuffer(out _, out var info);
    sum += (int)info.Id;
}

Console.WriteLine($"Total writes: {sum:N0}");