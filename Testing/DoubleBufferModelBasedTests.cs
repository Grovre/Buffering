using System;
using System.Collections.Generic;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferModelBasedTests
{
    private sealed class DoubleBufferModel<T>
    {
        public T Front { get; private set; }
        public T Back { get; private set; }
        public bool HasPendingUpdate { get; private set; }
        private readonly DoubleBufferSwapEffect _swapEffect;

        public DoubleBufferModel(T initialFront, T initialBack, DoubleBufferSwapEffect swapEffect)
        {
            Front = initialFront;
            Back = initialBack;
            HasPendingUpdate = true;
            _swapEffect = swapEffect;
        }

        public T ReadFront() => Front;

        public T ReadBack() => Back;

        public void UpdateBack(T value)
        {
            Back = value;
            HasPendingUpdate = true;
        }

        public bool Swap()
        {
            if (!HasPendingUpdate)
                return false;

            switch (_swapEffect)
            {
                case DoubleBufferSwapEffect.FlipRefOrValue:
                    (Front, Back) = (Back, Front);
                    break;
                case DoubleBufferSwapEffect.CopyRefOrValue:
                    Front = Back;
                    break;
                default:
                    throw new NotSupportedException();
            }

            HasPendingUpdate = false;
            return true;
        }
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue, 12345)]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue, 67890)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue, 12345)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue, 67890)]
    public void ModelBased_RandomOperations_MatchReferenceModel(DoubleBufferSwapEffect effect, int seed)
    {
        var random = new Random(seed);
        var buffer = new DoubleBuffer<int>(0, 0, effect);
        var model = new DoubleBufferModel<int>(0, 0, effect);

        const int operationsCount = 50_000;

        for (int op = 0; op < operationsCount; op++)
        {
            int action = random.Next(4); // 0: ReadFront, 1: ReadBack, 2: UpdateBack, 3: Swap

            switch (action)
            {
                case 0:
                    var actualFront = buffer.FrontReader.ReadFrontBuffer();
                    var expectedFront = model.ReadFront();
                    Assert.That(actualFront, Is.EqualTo(expectedFront), $"Mismatch in ReadFront at op {op}");
                    break;

                case 1:
                    var actualBack = buffer.BackWriter.ReadBackBuffer();
                    var expectedBack = model.ReadBack();
                    Assert.That(actualBack, Is.EqualTo(expectedBack), $"Mismatch in ReadBack at op {op}");
                    break;

                case 2:
                    int val = random.Next(1, 100_000);
                    buffer.BackWriter.UpdateBackBuffer(val);
                    model.UpdateBack(val);
                    break;

                case 3:
                    bool actualSwapped = buffer.BackWriter.SwapBuffers();
                    bool expectedSwapped = model.Swap();
                    Assert.That(actualSwapped, Is.EqualTo(expectedSwapped), $"Mismatch in Swap return at op {op}");
                    Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.EqualTo(model.ReadFront()), $"Mismatch in Front after Swap at op {op}");
                    Assert.That(buffer.BackWriter.ReadBackBuffer(), Is.EqualTo(model.ReadBack()), $"Mismatch in Back after Swap at op {op}");
                    break;
            }
        }
    }

    [Test]
    [TestCase(DoubleBufferSwapEffect.FlipRefOrValue, 54321)]
    [TestCase(DoubleBufferSwapEffect.CopyRefOrValue, 54321)]
    public void ModelBased_ReferenceTypes_RandomOperations_MatchReferenceModel(DoubleBufferSwapEffect effect, int seed)
    {
        var random = new Random(seed);
        var initialA = new TestObject(1, "A", 10);
        var initialB = new TestObject(2, "B", 20);

        var buffer = new DoubleBuffer<TestObject>(initialA, initialB, effect);
        var model = new DoubleBufferModel<TestObject>(initialA, initialB, effect);

        var objectPool = new List<TestObject> { initialA, initialB };
        for (int i = 3; i <= 20; i++)
        {
            objectPool.Add(new TestObject(i, $"Obj_{i}", i * 10));
        }

        const int operationsCount = 30_000;

        for (int op = 0; op < operationsCount; op++)
        {
            int action = random.Next(4);

            switch (action)
            {
                case 0:
                    var actualFront = buffer.FrontReader.ReadFrontBuffer();
                    var expectedFront = model.ReadFront();
                    Assert.That(actualFront, Is.SameAs(expectedFront), $"Mismatch in ReadFront (Ref) at op {op}");
                    break;

                case 1:
                    var actualBack = buffer.BackWriter.ReadBackBuffer();
                    var expectedBack = model.ReadBack();
                    Assert.That(actualBack, Is.SameAs(expectedBack), $"Mismatch in ReadBack (Ref) at op {op}");
                    break;

                case 2:
                    var selectedObj = objectPool[random.Next(objectPool.Count)];
                    buffer.BackWriter.UpdateBackBuffer(selectedObj);
                    model.UpdateBack(selectedObj);
                    break;

                case 3:
                    bool actualSwapped = buffer.BackWriter.SwapBuffers();
                    bool expectedSwapped = model.Swap();
                    Assert.That(actualSwapped, Is.EqualTo(expectedSwapped), $"Mismatch in Swap return (Ref) at op {op}");
                    Assert.That(buffer.FrontReader.ReadFrontBuffer(), Is.SameAs(model.ReadFront()), $"Mismatch in Front after Swap (Ref) at op {op}");
                    Assert.That(buffer.BackWriter.ReadBackBuffer(), Is.SameAs(model.ReadBack()), $"Mismatch in Back after Swap (Ref) at op {op}");
                    break;
            }
        }
    }
}
