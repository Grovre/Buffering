using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferInitializationTests
{
    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_SetsInitialFrontAndBackValues_ValueType(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(10, 20, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(20));
        Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(buffer.BackWriter.ReadBackBuffer(), Is.EqualTo(20));
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_SetsInitialFrontAndBackValues_ReferenceType(DoubleBufferSwapEffect effect)
    {
        var frontObj = new TestObject(1, "front", 100);
        var backObj = new TestObject(2, "back", 200);

        var buffer = new DoubleBuffer<TestObject>(frontObj, backObj, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.SameAs(frontObj));
        Assert.That(buffer.ReadBackBuffer(), Is.SameAs(backObj));
        Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.SameAs(frontObj));
        Assert.That(buffer.BackWriter.ReadBackBuffer(), Is.SameAs(backObj));
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_PermitsNullInitialValues_BothNull(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<string?>(null, null, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.Null);
        Assert.That(buffer.ReadBackBuffer(), Is.Null);
        Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.Null);
        Assert.That(buffer.BackWriter.ReadBackBuffer(), Is.Null);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_PermitsNullInitialValues_FrontNull(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<string?>(null, "back", effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.Null);
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo("back"));
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_PermitsNullInitialValues_BackNull(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<string?>("front", null, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo("front"));
        Assert.That(buffer.ReadBackBuffer(), Is.Null);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_PermitsNullableValueTypes(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int?>(null, 42, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.Null);
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(42));
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_WithDefaultStructValues_InitializesCorrectly(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<Struct64B>(default, new Struct64B(99), effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(default(Struct64B)));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(new Struct64B(99)));
    }

    [Test]
    public void Constructor_InitializesPendingUpdateFlagToTrue_PermitsImmediateInitialSwap_Flip()
    {
        var buffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.FlipRefOrValue);

        // Immediate swap without UpdateBackBuffer must succeed because _hasPendingUpdate is initially true
        bool swapped = buffer.SwapBuffers();

        Assert.That(swapped, Is.True);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(10));
    }

    [Test]
    public void Constructor_InitializesPendingUpdateFlagToTrue_PermitsImmediateInitialSwap_Copy()
    {
        var buffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.CopyRefOrValue);

        // Immediate swap without UpdateBackBuffer must succeed because _hasPendingUpdate is initially true
        bool swapped = buffer.SwapBuffers();

        Assert.That(swapped, Is.True);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(20));
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void Constructor_AfterInitialSwap_SecondSwapWithoutUpdateReturnsFalse(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(10, 20, effect);

        bool firstSwap = buffer.SwapBuffers();
        bool secondSwap = buffer.SwapBuffers();

        Assert.That(firstSwap, Is.True);
        Assert.That(secondSwap, Is.False);
    }

    [Test]
    public void FrontReaderProperty_ReturnsNonNullHandleBoundToBuffer()
    {
        var buffer = new DoubleBuffer<int>(100, 200, DoubleBufferSwapEffect.FlipRefOrValue);
        var reader = buffer.FrontReader;

        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(100));
    }

    [Test]
    public void BackWriterProperty_ReturnsNonNullHandleBoundToBuffer()
    {
        var buffer = new DoubleBuffer<int>(100, 200, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;

        Assert.That(writer, Is.Not.Null);
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(200));
    }

    [Test]
    public void FrontReaderProperty_ReturnsFreshInstanceOnEachAccess()
    {
        var buffer = new DoubleBuffer<int>(1, 2, DoubleBufferSwapEffect.FlipRefOrValue);

        var reader1 = buffer.FrontReader;
        var reader2 = buffer.FrontReader;

        Assert.That(reader1, Is.Not.SameAs(reader2));
        Assert.That(reader1.ReadFrontBuffer(), Is.EqualTo(reader2.ReadFrontBuffer()));
    }

    [Test]
    public void BackWriterProperty_ReturnsFreshInstanceOnEachAccess()
    {
        var buffer = new DoubleBuffer<int>(1, 2, DoubleBufferSwapEffect.FlipRefOrValue);

        var writer1 = buffer.BackWriter;
        var writer2 = buffer.BackWriter;

        Assert.That(writer1, Is.Not.SameAs(writer2));
        Assert.That(writer1.ReadBackBuffer(), Is.EqualTo(writer2.ReadBackBuffer()));
    }
}
