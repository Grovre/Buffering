// See https://aka.ms/new-console-template for more information

using Buffering.DoubleBuffering;
using Buffering.Locking.Locks;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferTests
{
    private DoubleBuffer<int> _doubleBuffer = null!;
    private DoubleBufferBackController<int> _backController;
    private DoubleBufferFrontReader<int> _frontReader;

    [SetUp]
    public void SetUp()
    {
        _doubleBuffer = new DoubleBuffer<int>(new DoubleBufferConfiguration(DoubleBufferSwapEffect.Flip, new NoLock()));
        _backController = _doubleBuffer.BackController;
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
}
