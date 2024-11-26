# Buffering
This library provides very easy and streamlined functionality for implementing different kinds of buffers in any system.
A double buffer that is configurable for every step of the way is provided in this library.

# Threadsafe
All buffers are implemented in a threadsafe manner using the most efficient object-oriented implementation involving a lock and handle that only needs to be disposed after you're done.
However, there is also a no-cost NoLock implementation that can be used as well if synchronization isn't a concern.

# Double Buffer Example
Here is an example of how simple it is to set up a double buffer.
This creates a double buffer and a cancellation token source to define a runtime length of the program:
```cs
var db = new DoubleBuffer<object>(
    new SystemThreadingLock(),
    DoubleBufferSwapEffect.Flip);

var cts = new CancellationTokenSource(10_000);
```

This next part creates the long-running back buffer update thread where the back buffer is updated:
```cs
var bufferUpdateTask = new TaskFactory(TaskCreationOptions.LongRunning, 0).StartNew(() =>
{
    var token = cts.Token;
    var writer = db.BackWriter;
    while (!token.IsCancellationRequested)
    {
        var obj = GetResult();
        writer.UpdateBackBuffer(obj);
        writer.SwapBuffers();
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

That's all there is to creating a double buffer and using it at very performant speeds with minimal or no lock times.
