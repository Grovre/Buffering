using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferPendingUpdateTests
{
    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void SwapBuffers_InitialStateHasPendingUpdate_ReturnsTrue(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(1, 2, effect);
        bool swapped = buffer.SwapBuffers();

        Assert.That(swapped, Is.True);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void SwapBuffers_MultipleConsecutiveSwapsWithoutUpdate_AllReturnFalseExceptFirst(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(1, 2, effect);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        bool firstSwap = writer.SwapBuffers();
        Assert.That(firstSwap, Is.True);
        var frontAfterFirst = reader.ReadFrontBuffer();
        var backAfterFirst = writer.ReadBackBuffer();

        for (int i = 0; i < 20; i++)
        {
            bool subsequentSwap = writer.SwapBuffers();
            Assert.That(subsequentSwap, Is.False, $"Swap attempt {i + 2} should have returned false");
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(frontAfterFirst));
            Assert.That(writer.ReadBackBuffer(), Is.EqualTo(backAfterFirst));
        }
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void UpdateBackBuffer_SetsPendingUpdateFlag_AllowsNextSwap(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(1, 2, effect);
        var writer = buffer.BackWriter;

        // Consume initial pending update
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(writer.SwapBuffers(), Is.False);

        // Update back buffer
        writer.UpdateBackBuffer(10);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(writer.SwapBuffers(), Is.False);

        // Update again
        writer.UpdateBackBuffer(20);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(writer.SwapBuffers(), Is.False);
    }

    [Test]
    public void MultipleUpdatesBeforeSwap_OverwritesBack_SingleSwapPublishesLatest()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Initial swap
        writer.SwapBuffers();

        // Stage 100 updates
        for (int i = 1; i <= 100; i++)
        {
            writer.UpdateBackBuffer(i);
        }

        // Only one swap needed
        bool swapped = writer.SwapBuffers();
        Assert.That(swapped, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(100));

        // Subsequent swap without new update is no-op
        Assert.That(writer.SwapBuffers(), Is.False);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void UpdateBackBuffer_WithSameValue_SetsPendingFlag(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(5, 5, effect);
        var writer = buffer.BackWriter;

        // Consume initial pending update
        writer.SwapBuffers();
        Assert.That(writer.SwapBuffers(), Is.False);

        // Write exact same value 5
        writer.UpdateBackBuffer(5);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(writer.SwapBuffers(), Is.False);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void ReadBackBuffer_DoesNotSetPendingFlag(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(1, 2, effect);
        var writer = buffer.BackWriter;

        writer.SwapBuffers(); // pending flag is now false

        _ = writer.ReadBackBuffer();
        _ = writer.ReadBackBuffer();

        Assert.That(writer.SwapBuffers(), Is.False);
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue)]
    public void ReadFrontBuffer_DoesNotSetPendingFlag(DoubleBufferSwapEffect effect)
    {
        var buffer = new DoubleBuffer<int>(1, 2, effect);
        var reader = buffer.FrontReader;
        var writer = buffer.BackWriter;

        writer.SwapBuffers(); // pending flag is now false

        _ = reader.ReadFrontBuffer();
        _ = reader.ReadFrontBuffer();

        Assert.That(writer.SwapBuffers(), Is.False);
    }

    [Test]
    public void InPlaceMutation_WithoutUpdateBackBuffer_SwapReturnsFalse()
    {
        var obj1 = new TestObject(1, "A", 10);
        var obj2 = new TestObject(2, "B", 20);

        var buffer = new DoubleBuffer<TestObject>(obj1, obj2, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Consume initial swap
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj2));

        // Writer reads back buffer (obj1) and mutates in-place, but forgets to call UpdateBackBuffer
        var backObj = writer.ReadBackBuffer();
        backObj.Value = 999;

        // Swap must return false because pending update flag was never set
        bool swapped = writer.SwapBuffers();
        Assert.That(swapped, Is.False);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj2)); // Front unchanged
    }

    [Test]
    public void InPlaceMutation_WithUpdateBackBuffer_SwapReturnsTrueAndPublishes()
    {
        var obj1 = new TestObject(1, "A", 10);
        var obj2 = new TestObject(2, "B", 20);

        var buffer = new DoubleBuffer<TestObject>(obj1, obj2, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Consume initial swap
        writer.SwapBuffers();

        // Writer reads back buffer (obj1), mutates it, and calls UpdateBackBuffer
        var backObj = writer.ReadBackBuffer();
        backObj.Value = 999;
        writer.UpdateBackBuffer(backObj);

        // Swap succeeds and publishes obj1
        bool swapped = writer.SwapBuffers();
        Assert.That(swapped, Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.SameAs(obj1));
        Assert.That(reader.ReadFrontBuffer().Value, Is.EqualTo(999));
    }

    [Test]
    public void ComplexInterleavedUpdatesAndSwaps_MaintainsConsistency()
    {
        var buffer = new DoubleBuffer<int>(0, 0, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // 1. Initial swap -> true, front=0
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(0));

        // 2. Redundant swap -> false, front=0
        Assert.That(writer.SwapBuffers(), Is.False);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(0));

        // 3. Update 1 -> Swap -> true, front=1
        writer.UpdateBackBuffer(1);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(1));

        // 4. Update 2, 3, 4 -> Swap -> true, front=4
        writer.UpdateBackBuffer(2);
        writer.UpdateBackBuffer(3);
        writer.UpdateBackBuffer(4);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(4));

        // 5. Redundant swap -> false, front=4
        Assert.That(writer.SwapBuffers(), Is.False);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(4));

        // 6. Update 5 -> Swap -> true, front=5
        writer.UpdateBackBuffer(5);
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(5));

        // 7. Redundant swap x3 -> false, front=5
        Assert.That(writer.SwapBuffers(), Is.False);
        Assert.That(writer.SwapBuffers(), Is.False);
        Assert.That(writer.SwapBuffers(), Is.False);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(5));
    }
}
