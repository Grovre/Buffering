using System;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferSwapEffectTests
{
    [Test]
    public void FlipRefOrValue_ValueType_ExchangesFrontAndBack()
    {
        var buffer = new DoubleBuffer<int>(100, 200, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Swap 1: Exchanges 100 and 200
        bool swapped1 = writer.SwapBuffers();
        Assert.That(swapped1, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(200));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(100));

        // Update back with 300 and Swap 2
        writer.UpdateBackBuffer(300);
        bool swapped2 = writer.SwapBuffers();
        Assert.That(swapped2, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(300));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(200));

        // Update back with 400 and Swap 3
        writer.UpdateBackBuffer(400);
        bool swapped3 = writer.SwapBuffers();
        Assert.That(swapped3, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(400));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(300));
    }

    [Test]
    public void FlipRefOrValue_ReferenceType_ExchangesPointersWithoutReallocating()
    {
        var objA = new TestObject(1, "A", 10);
        var objB = new TestObject(2, "B", 20);

        var buffer = new DoubleBuffer<TestObject>(objA, objB, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Before swap
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(objA));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(objB));

        // Swap 1
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(objB));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(objA));

        // Swap 2 with re-staged objA
        writer.UpdateBackBuffer(objA);
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(objA));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(objB));
    }

    [Test]
    public void FlipRefOrValue_PingPongRecycling_CyclesBetweenTwoInstances()
    {
        var objA = new TestObject(1, "A", 0);
        var objB = new TestObject(2, "B", 0);

        var buffer = new DoubleBuffer<TestObject>(objA, objB, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        for (int frame = 1; frame <= 20; frame++)
        {
            var recycled = writer.ReadBackBuffer();
            recycled.Value = frame * 10;
            writer.UpdateBackBuffer(recycled);
            writer.SwapBuffers();

            var published = reader.ReadFrontBuffer();
            Assert.That(published, Is.SameAs(recycled));
            Assert.That(published.Value, Is.EqualTo(frame * 10));

            // Odd frames should publish objB (since objA was front initially), even frames objA
            if (frame % 2 == 1)
            {
                Assert.That(published, Is.SameAs(objB));
                Assert.That(writer.ReadBackBuffer(), Is.SameAs(objA));
            }
            else
            {
                Assert.That(published, Is.SameAs(objA));
                Assert.That(writer.ReadBackBuffer(), Is.SameAs(objB));
            }
        }
    }

    [Test]
    public void CopyRefOrValue_ValueType_CopiesBackToFront_BackRetainsValue()
    {
        var buffer = new DoubleBuffer<int>(10, 20, DoubleBufferSwapEffect.CopyRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Swap 1: front gets 20, back retains 20
        bool swapped1 = writer.SwapBuffers();
        Assert.That(swapped1, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(20));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(20));

        // Update back with 50 and swap
        writer.UpdateBackBuffer(50);
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(50));
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(20)); // Front not modified until swap

        bool swapped2 = writer.SwapBuffers();
        Assert.That(swapped2, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(50));
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(50));
    }

    [Test]
    public void CopyRefOrValue_ReferenceType_CopiesPointer_BothSlotsAliasSameInstance()
    {
        var obj1 = new TestObject(1, "First", 100);
        var obj2 = new TestObject(2, "Second", 200);

        var buffer = new DoubleBuffer<TestObject>(obj1, obj2, DoubleBufferSwapEffect.CopyRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        writer.SwapBuffers();

        // Both front and back point to obj2
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj2));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(obj2));

        // Mutating obj2 in-place affects both because of pointer aliasing
        obj2.Value = 999;
        Assert.That(reader.ReadFrontBuffer().Value, Is.EqualTo(999));
        Assert.That(writer.ReadBackBuffer().Value, Is.EqualTo(999));

        // Assigning a new instance to back breaks the alias for back, but front remains obj2
        var obj3 = new TestObject(3, "Third", 300);
        writer.UpdateBackBuffer(obj3);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj2));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(obj3));

        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj3));
        Assert.That(writer.ReadBackBuffer(), Is.SameAs(obj3));
    }

    [Test]
    public void CopyRefOrValue_IncrementalStateAccumulation_WorldStateStruct()
    {
        var buffer = new DoubleBuffer<WorldState>(
            new WorldState(0, 0.0f, 0),
            new WorldState(0, 0.0f, 0),
            DoubleBufferSwapEffect.CopyRefOrValue);

        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        for (int i = 1; i <= 10; i++)
        {
            var state = writer.ReadBackBuffer();
            state.EntityCount += 5;
            state.SimulationTime += 0.016f;
            state.Tick = i;

            writer.UpdateBackBuffer(state);
            writer.SwapBuffers();

            var front = reader.ReadFrontBuffer();
            var back = writer.ReadBackBuffer();

            Assert.That(front.EntityCount, Is.EqualTo(i * 5));
            Assert.That(front.Tick, Is.EqualTo(i));
            Assert.That(back.EntityCount, Is.EqualTo(i * 5));
            Assert.That(back.Tick, Is.EqualTo(i));
        }
    }

    [Test]
    public void SwapBuffers_UnsupportedSwapEffect_ThrowsNotSupportedException()
    {
        var invalidEffect = (DoubleBufferSwapEffect)999;
        var buffer = new DoubleBuffer<int>(1, 2, invalidEffect);

        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            buffer.SwapBuffers();
        });

        Assert.That(ex!.Message, Does.Contain("Unsupported swap effect: 999"));
    }

    [Test]
    public void SwapBuffers_NegativeSwapEffect_ThrowsNotSupportedException()
    {
        var invalidEffect = (DoubleBufferSwapEffect)(-1);
        var buffer = new DoubleBuffer<int>(1, 2, invalidEffect);

        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            buffer.SwapBuffers();
        });

        Assert.That(ex!.Message, Does.Contain("Unsupported swap effect: -1"));
    }
}
