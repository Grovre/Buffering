# Buffering
This library provides very easy and streamlined functionality for implementing different kinds of buffers in any system.
Using the package, you will have a single buffer and double buffer for wherever you need these in your code. There are
also methods for easily partitioning data and optionally processing the partitioned data in parallel without relying
on a task scheduler and unnecessary synchronization in most cases from the BCL-provided
System.Threading.Tasks.Parallel class.

# Threadsafe
All buffers are implemented in a threadsafe manner using the most efficient object-oriented implementation involving a lock and handle that only needs to be disposed after you're done.

# Double Buffer Example
Here is an example of how simple it is to set up a double buffer. A single buffer is very similar but uses the respectively named classes:

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
    var controller = db.BackController;
    while (!token.IsCancellationRequested)
    {
        controller.UpdateBackBuffer();
        controller.SwapBuffers();
    }
});
```

Finally, the portion of the program that reads the buffer until the task is over and it updates no more:
```cs
var reader = db.FrontReader;
while (!bufferUpdateTask.IsCompleted)
{
    reader.ReadFrontBuffer(out var rsc, out var rscInfo).Dispose();
    Console.WriteLine($"{rscInfo.Id:N0}: {rsc} : {rscInfo}");
}
```

That's all there is to creating a double buffer and using it at very performant speeds with minimal lock times.
