using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferHandleTests
{
    [Test]
    public void FrontReader_ReadFrontBuffer_DelegatesToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<int>(42, 84, DoubleBufferSwapEffect.FlipRefOrValue);
        var reader = buffer.FrontReader;

        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(buffer.ReadFrontBuffer()));

        buffer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(buffer.ReadFrontBuffer()));
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(84));
    }

    [Test]
    public void BackWriter_ReadBackBuffer_DelegatesToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<int>(42, 84, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;

        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(buffer.ReadBackBuffer()));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(84));
    }

    [Test]
    public void BackWriter_UpdateBackBuffer_DelegatesToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<int>(42, 84, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;

        writer.UpdateBackBuffer(999);
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(999));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(999));
    }

    [Test]
    public void BackWriter_SwapBuffers_DelegatesToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<int>(42, 84, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;

        bool swapped = writer.SwapBuffers();
        Assert.That(swapped, Is.True);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(84));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(42));
    }

    [Test]
    public void FrontReader_DirectConstructor_BindsToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<string>("front", "back", DoubleBufferSwapEffect.FlipRefOrValue);
        var directReader = new DoubleBufferFrontReader<string>(buffer);

        Assert.That(directReader.ReadFrontBuffer(), Is.EqualTo("front"));

        buffer.SwapBuffers();
        Assert.That(directReader.ReadFrontBuffer(), Is.EqualTo("back"));
    }

    [Test]
    public void BackWriter_DirectConstructor_BindsToDoubleBuffer()
    {
        var buffer = new DoubleBuffer<string>("front", "back", DoubleBufferSwapEffect.FlipRefOrValue);
        var directWriter = new DoubleBufferBackWriter<string>(buffer);

        Assert.That(directWriter.ReadBackBuffer(), Is.EqualTo("back"));

        directWriter.UpdateBackBuffer("new_back");
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo("new_back"));

        bool swapped = directWriter.SwapBuffers();
        Assert.That(swapped, Is.True);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo("new_back"));
    }

    [Test]
    public void MultipleFrontReaders_ReadConsistentData()
    {
        var buffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.FlipRefOrValue);
        var reader1 = buffer.FrontReader;
        var reader2 = buffer.FrontReader;
        var reader3 = new DoubleBufferFrontReader<int>(buffer);

        Assert.That(reader1.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(reader2.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(reader3.ReadFrontBuffer(), Is.EqualTo(10));

        buffer.SwapBuffers();

        Assert.That(reader1.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(reader2.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(reader3.ReadFrontBuffer(), Is.EqualTo(20));
    }

    [Test]
    public void HandleCaching_MaintainsValidBindingAcrossManyCycles()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        // Cache handles locally (as recommended in README for best performance)
        var cachedReader = buffer.FrontReader;
        var cachedWriter = buffer.BackWriter;

        for (int i = 1; i <= 5_000; i++)
        {
            cachedWriter.UpdateBackBuffer(i);
            cachedWriter.SwapBuffers();

            var read = cachedReader.ReadFrontBuffer();
            Assert.That(read, Is.EqualTo(i));
        }
    }
}
