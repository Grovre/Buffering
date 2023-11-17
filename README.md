# Buffering
This library provides very easy and streamlined functionality for implementing different kinds of buffers in any system.
Currently, there is only a double buffer to maximize throughput between a source and destination with the most up-to-date information from the source.

# Threadsafe
All buffers are implemented in a threadsafe manner using the most efficient object-oriented implementation involving a lock and handle that only needs to be disposed after you're done.

# Double Buffer Example
Here is an example of how simple it is to set up a double buffer:

This creates a double buffer using the given configuration (yeah, multiple ways you can define how a buffer works) and a cancellation token source to define a runtime length of the program:
```cs
var db = new DoubleBuffer<Vector3>(
    rsc: new BufferingResource<Vector3>(
        init: () => Vector3.Zero,
        updater: (ref Vector3 rsc, bool _) => rsc = new Vector3(rsc.X + 1F)),
    configuration: new DoubleBufferConfiguration(
        lockImpl: new MonitorLock(),
        swapEffect: DoubleBufferSwapEffect.Flip));

var cts = new CancellationTokenSource(10_000);
```

This next part creates the long-running back buffer update thread:
```cs
var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    while (!token.IsCancellationRequested)
    {
        db.UpdateBackBuffer();
        db.SwapBuffers();
    }
});
```

Finally, the portion of the program that reads the buffer until the task is over and it updates no more:
```cs
while (!bufferUpdateTask.IsCompleted)
{
    db.ReadFrontBuffer(out var rsc, out var rscInfo).Dispose(); // Dispose of the lock returned
    Console.WriteLine($"{rscInfo.Id:N0}: {rsc} : {rscInfo}");
}
```

That's all there is to creating a double buffer and using it at very performant speeds with minimal lock times.
