using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferMatrixTests
{
    [Test]
    [Combinatorial]
    public void Matrix_SequenceDepths_And_SwapEffects(
        [Values(DoubleBufferSwapEffect.FlipRefOrValue, DoubleBufferSwapEffect.CopyRefOrValue)] DoubleBufferSwapEffect effect,
        [Values(1, 2, 5, 10, 25, 50)] int updatesBeforeSwap,
        [Values(1, 3, 5)] int cycles)
    {
        var buffer = new DoubleBuffer<int>(0, 0, effect);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        // Consume initial swap
        Assert.That(writer.SwapBuffers(), Is.True);
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(0));

        int globalCounter = 0;

        for (int c = 0; c < cycles; c++)
        {
            int lastWritten = 0;
            for (int u = 0; u < updatesBeforeSwap; u++)
            {
                globalCounter++;
                lastWritten = globalCounter;
                writer.UpdateBackBuffer(lastWritten);
            }

            // Before swap, front has not updated
            if (c == 0 && updatesBeforeSwap > 1)
            {
                Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(0));
            }

            // Swap
            bool swapped = writer.SwapBuffers();
            Assert.That(swapped, Is.True);

            // Front has published the last written value
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(lastWritten));

            // Immediate redundant swap is false
            Assert.That(writer.SwapBuffers(), Is.False);
            Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(lastWritten));
        }
    }

    [Test]
    [Combinatorial]
    public void Matrix_InitialStatePermutations(
        [Values(DoubleBufferSwapEffect.FlipRefOrValue, DoubleBufferSwapEffect.CopyRefOrValue)] DoubleBufferSwapEffect effect,
        [Values(-100, 0, 100)] int initialFront,
        [Values(-200, 0, 200)] int initialBack)
    {
        var buffer = new DoubleBuffer<int>(initialFront, initialBack, effect);

        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialFront));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(initialBack));

        bool swapped = buffer.SwapBuffers();
        Assert.That(swapped, Is.True);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialBack));

        if (effect == DoubleBufferSwapEffect.FlipRefOrValue)
        {
            Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(initialFront));
        }
        else
        {
            Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(initialBack));
        }
    }
}
