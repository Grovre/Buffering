// See https://aka.ms/new-console-template for more information

using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferTests
{
    private DoubleBuffer<int> _doubleBuffer = null!;
    private DoubleBufferBackWriter<int> _backController;
    private DoubleBufferFrontReader<int> _frontReader;

    [SetUp]
    public void SetUp()
    {
        _doubleBuffer = new DoubleBuffer<int>(new NoLock(), DoubleBufferSwapEffect.Flip);
        _backController = _doubleBuffer.BackWriter;
        _frontReader = _doubleBuffer.FrontReader;
    }

    [Test]
    public void TestUpdateBackBuffer()
    {
        var handle = _frontReader.ReadFrontBuffer(out var _, out var info);
        handle.Dispose();
        Assert.That(info.FromBuffer, Is.EqualTo(false));

        _backController.UpdateBackBuffer(42);
        _backController.SwapBuffers();

        handle = _frontReader.ReadFrontBuffer(out var rsc, out info);
        Assert.That(info.FromBuffer, Is.EqualTo(true));
        Assert.That(rsc, Is.EqualTo(42));
        handle.Dispose();
    }

    [Test]
    public void TestSwapBuffers()
    {
        _backController.UpdateBackBuffer(42);
        _backController.SwapBuffers();

        var handle = _frontReader.ReadFrontBuffer(out var rsc, out var info);
        Assert.That(rsc, Is.EqualTo(42));
        handle.Dispose();

        _backController.UpdateBackBuffer(84);
        _backController.SwapBuffers();

        handle = _frontReader.ReadFrontBuffer(out rsc, out info);
        Assert.That(rsc, Is.EqualTo(84));
        handle.Dispose();

        _backController.SwapBuffers();
        handle = _frontReader.ReadFrontBuffer(out rsc, out info);
        Assert.That(rsc, Is.EqualTo(42));
        handle.Dispose();
    }

    [Test]
    public void TestReadFrontBuffer()
    {
        _backController.UpdateBackBuffer(42);
        _backController.SwapBuffers();

        var handle = _frontReader.ReadFrontBuffer(out var rsc, out var info);
        Assert.That(rsc, Is.EqualTo(42));
        handle.Dispose();
    }
    [Test]
    public void TestReadBackBuffer()
    {
        // Update back buffer and read it
        _backController.UpdateBackBuffer(42);
        ref var backBuffer = ref _backController.ReadBackBuffer();
        Assert.That(backBuffer, Is.EqualTo(42));

        // Modify back buffer and ensure the change is reflected
        backBuffer = 84;
        ref var modifiedBackBuffer = ref _backController.ReadBackBuffer();
        Assert.That(modifiedBackBuffer, Is.EqualTo(84));

        // Swap buffers and ensure front buffer has the updated value
        _backController.SwapBuffers();
        var handle = _frontReader.ReadFrontBuffer(out var frontBuffer, out var info);
        Assert.That(frontBuffer, Is.EqualTo(84));
        handle.Dispose();

        // Ensure back buffer is safe to access after swap
        _backController.UpdateBackBuffer(21);
        ref var newBackBuffer = ref _backController.ReadBackBuffer();
        Assert.That(newBackBuffer, Is.EqualTo(21));
    }


}
