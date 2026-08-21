using System;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferEdgeCasesTests
{
    [Test]
    public void RapidUpdatesWithoutSwap_PublishesOnlyLatest()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Consume initial swap
        writer.SwapBuffers();

        // 5,000 updates without a swap
        for (int i = 1; i <= 5_000; i++)
        {
            writer.UpdateBackBuffer(i);
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(0)); // Front remains 0
        }

        // Single swap publishes 5000
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(5_000));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(0));
    }

    [Test]
    public void RapidSwapsWithoutUpdates_AllSubsequentAreNoOps()
    {
        var buffer = new DoubleBuffer<int>(100, 200, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(200));

        for (int i = 0; i < 1_000; i++)
        {
            Assert.That(writer.SwapBuffers(), Is.False);
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(200));
            Assert.That(writer.ReadBackBuffer(), Is.EqualTo(100));
        }
    }

    [Test]
    public void SwappingIdenticalValues_ValueType()
    {
        var buffer = new DoubleBuffer<int>(42, 42, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(42));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(42));
    }

    [Test]
    public void SwappingSameObjectReference_FrontEqualsBack()
    {
        var sharedObj = new TestObject(1, "Shared", 100);
        var buffer = new DoubleBuffer<TestObject>(sharedObj, sharedObj, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(sharedObj));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(sharedObj));

        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(sharedObj));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(sharedObj));
    }

    [Test]
    public void ComplexNullTransitions_ReferenceTypes()
    {
        var obj1 = new TestObject(1, "Obj1", 10);
        var obj2 = new TestObject(2, "Obj2", 20);

        var buffer = new DoubleBuffer<TestObject?>(null, null, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // null <-> null
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.Null);
        Assert.That(writer.ReadBackBuffer(), Is.Null);

        // Update back with obj1
        writer.UpdateBackBuffer(obj1);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj1));
        Assert.That(writer.ReadBackBuffer(), Is.Null);

        // Update back with obj2
        writer.UpdateBackBuffer(obj2);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj2));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(obj1));

        // Update back with null
        writer.UpdateBackBuffer(null);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.Null);
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(obj2));
    }

    [Test]
    public void LargePayloadBuffer_Array()
    {
        const int size = 1_000_000; // 1 MB
        var arr1 = new byte[size];
        var arr2 = new byte[size];
        arr1[0] = 1;
        arr2[0] = 2;

        var buffer = new DoubleBuffer<byte[]>(arr1, arr2, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer()[0], Is.EqualTo(1));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer()[0], Is.EqualTo(2));
        Assert.That(buffer.ReadBackBuffer()[0], Is.EqualTo(1));
    }

    [Test]
    public void HighIterationSequentialStress_100_000_Cycles()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        for (int i = 1; i <= 100_000; i++)
        {
            writer.UpdateBackBuffer(i);
            bool swapped = writer.SwapBuffers();
            Assert.That(swapped, Is.True);
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(i));
        }
    }
}
