using System;
using System.Collections.Generic;
using Buffering.DoubleBuffering;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class DoubleBufferTypeVariationsTests
{
    // ──────────────────────────────────────────────
    // Numeric Primitives & Boundaries
    // ──────────────────────────────────────────────

    [Test]
    [TestCase(byte.MinValue, byte.MaxValue, (byte)128)]
    public void Byte_Variations(byte initialFront, byte initialBack, byte update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(sbyte.MinValue, sbyte.MaxValue, (sbyte)0)]
    public void SByte_Variations(sbyte initialFront, sbyte initialBack, sbyte update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(short.MinValue, short.MaxValue, (short)1234)]
    public void Short_Variations(short initialFront, short initialBack, short update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(ushort.MinValue, ushort.MaxValue, (ushort)54321)]
    public void UShort_Variations(ushort initialFront, ushort initialBack, ushort update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(int.MinValue, int.MaxValue, 42)]
    public void Int_Variations(int initialFront, int initialBack, int update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(uint.MinValue, uint.MaxValue, 123456789U)]
    public void UInt_Variations(uint initialFront, uint initialBack, uint update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(long.MinValue, long.MaxValue, 987654321012345L)]
    public void Long_Variations(long initialFront, long initialBack, long update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(ulong.MinValue, ulong.MaxValue, 18446744073709551610UL)]
    public void ULong_Variations(ulong initialFront, ulong initialBack, ulong update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase(float.MinValue, float.MaxValue, float.NaN)]
    [TestCase(float.NegativeInfinity, float.PositiveInfinity, float.Epsilon)]
    public void Float_Variations(float initialFront, float initialBack, float update)
    {
        var buffer = new DoubleBuffer<float>(initialFront, initialBack, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialFront));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialBack));

        buffer.BackWriter.UpdateBackBuffer(update);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(update));
    }

    [Test]
    [TestCase(double.MinValue, double.MaxValue, double.NaN)]
    [TestCase(double.NegativeInfinity, double.PositiveInfinity, double.Epsilon)]
    public void Double_Variations(double initialFront, double initialBack, double update)
    {
        var buffer = new DoubleBuffer<double>(initialFront, initialBack, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialFront));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(initialBack));

        buffer.BackWriter.UpdateBackBuffer(update);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(update));
    }

    [Test]
    [TestCase(true, false, true)]
    [TestCase(false, true, false)]
    public void Bool_Variations(bool initialFront, bool initialBack, bool update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    [TestCase('A', 'Z', '★')]
    [TestCase('\0', '\uffff', 'ж')]
    public void Char_Variations(char initialFront, char initialBack, char update)
    {
        TestPrimitiveLifecycle(initialFront, initialBack, update);
    }

    [Test]
    public void Decimal_Variations()
    {
        TestPrimitiveLifecycle(decimal.MinValue, decimal.MaxValue, 123456789.987654321m);
    }

    [Test]
    public void NInt_And_NUInt_Variations()
    {
        TestPrimitiveLifecycle((nint)int.MinValue, (nint)int.MaxValue, (nint)42);
        TestPrimitiveLifecycle((nuint)0, (nuint)uint.MaxValue, (nuint)99);
    }

    [Test]
    public void Guid_Variations()
    {
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var g3 = Guid.NewGuid();
        TestPrimitiveLifecycle(g1, g2, g3);
    }

    [Test]
    public void DateTime_AndTimeSpan_Variations()
    {
        var dt1 = DateTime.UtcNow;
        var dt2 = dt1.AddDays(1);
        var dt3 = dt1.AddDays(2);
        TestPrimitiveLifecycle(dt1, dt2, dt3);

        var ts1 = TimeSpan.FromSeconds(10);
        var ts2 = TimeSpan.FromMinutes(5);
        var ts3 = TimeSpan.FromHours(1);
        TestPrimitiveLifecycle(ts1, ts2, ts3);
    }

    // ──────────────────────────────────────────────
    // Enums
    // ──────────────────────────────────────────────

    [Test]
    public void Enum_Variations()
    {
        TestPrimitiveLifecycle(SampleEnum.First, SampleEnum.Second, SampleEnum.Special);
        TestPrimitiveLifecycle(SampleFlags.None, SampleFlags.Read | SampleFlags.Write, SampleFlags.All);
    }

    // ──────────────────────────────────────────────
    // Nullable Value Types
    // ──────────────────────────────────────────────

    [Test]
    public void NullableInt_TransitionsBetweenNullAndValues()
    {
        var buffer = new DoubleBuffer<int?>(null, 10, DoubleBufferSwapEffect.FlipRefOrValue);
        var writer = buffer.BackWriter;
        var reader = buffer.FrontReader;

        Assert.That(reader.ReadFrontBuffer(), Is.Null);
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(10));

        // Swap 1: front=10, back=null
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(10));
        Assert.That(writer.ReadBackBuffer(), Is.Null);

        // Update with null
        writer.UpdateBackBuffer(null);
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.Null);
        Assert.That(writer.ReadBackBuffer(), Is.EqualTo(10));

        // Update with value
        writer.UpdateBackBuffer(999);
        writer.SwapBuffers();
        Assert.That(reader.ReadFrontBuffer(), Is.EqualTo(999));
        Assert.That(writer.ReadBackBuffer(), Is.Null);
    }

    [Test]
    public void NullableGuid_Transitions()
    {
        var g = Guid.NewGuid();
        var buffer = new DoubleBuffer<Guid?>(null, g, DoubleBufferSwapEffect.CopyRefOrValue);
        Assert.That(buffer.ReadFrontBuffer(), Is.Null);

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(g));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(g));

        buffer.BackWriter.UpdateBackBuffer(null);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.Null);
        Assert.That(buffer.ReadBackBuffer(), Is.Null);
    }

    // ──────────────────────────────────────────────
    // Structs of Various Sizes
    // ──────────────────────────────────────────────

    [Test]
    public void Struct_1Byte()
    {
        var buffer = new DoubleBuffer<Struct1B>(new Struct1B(1), new Struct1B(2), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(1));
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(2));
    }

    [Test]
    public void Struct_4Bytes()
    {
        var buffer = new DoubleBuffer<Struct4B>(new Struct4B(100), new Struct4B(200), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(100));
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(200));
    }

    [Test]
    public void Struct_8Bytes()
    {
        var buffer = new DoubleBuffer<Struct8B>(new Struct8B(1000L), new Struct8B(2000L), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(1000L));
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().Value, Is.EqualTo(2000L));
    }

    [Test]
    public void Struct_16Bytes()
    {
        var buffer = new DoubleBuffer<Struct16B>(new Struct16B(10), new Struct16B(20), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        Assert.That(buffer.ReadFrontBuffer().A, Is.EqualTo(10));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        Assert.That(buffer.ReadFrontBuffer().A, Is.EqualTo(20));
    }

    [Test]
    public void Struct_32Bytes()
    {
        var buffer = new DoubleBuffer<Struct32B>(new Struct32B(10), new Struct32B(20), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().A, Is.EqualTo(20));
    }

    [Test]
    public void Struct_64Bytes()
    {
        var buffer = new DoubleBuffer<Struct64B>(new Struct64B(100), new Struct64B(200), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        Assert.That(buffer.ReadFrontBuffer().A, Is.EqualTo(200));
    }

    [Test]
    public void Struct_128Bytes()
    {
        var buffer = new DoubleBuffer<Struct128B>(new Struct128B(500), new Struct128B(600), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        Assert.That(buffer.ReadFrontBuffer().Low.A, Is.EqualTo(600));
    }

    [Test]
    public void Struct_256Bytes()
    {
        var buffer = new DoubleBuffer<Struct256B>(new Struct256B(700), new Struct256B(800), DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().IsValid(), Is.True);
        Assert.That(buffer.ReadFrontBuffer().Low.Low.A, Is.EqualTo(800));
    }

    [Test]
    public void MutableStruct_ValueCopySemantics()
    {
        var s1 = new MutableStruct(1, 2);
        var s2 = new MutableStruct(3, 4);

        var buffer = new DoubleBuffer<MutableStruct>(s1, s2, DoubleBufferSwapEffect.FlipRefOrValue);

        // Modifying local variable does not affect buffer
        s1.X = 999;
        Assert.That(buffer.ReadFrontBuffer().X, Is.EqualTo(1));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().X, Is.EqualTo(3));
    }

    [Test]
    public void StructWithReferences_OperatesCorrectly()
    {
        var s1 = new StructWithReferences("First", new List<int> { 1, 2, 3 });
        var s2 = new StructWithReferences("Second", new List<int> { 4, 5, 6 });

        var buffer = new DoubleBuffer<StructWithReferences>(s1, s2, DoubleBufferSwapEffect.FlipRefOrValue);

        Assert.That(buffer.ReadFrontBuffer().Name, Is.EqualTo("First"));
        Assert.That(buffer.ReadFrontBuffer().Numbers, Is.EqualTo(new[] { 1, 2, 3 }));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer().Name, Is.EqualTo("Second"));
        Assert.That(buffer.ReadFrontBuffer().Numbers, Is.EqualTo(new[] { 4, 5, 6 }));
    }

    // ──────────────────────────────────────────────
    // Strings & Reference Types
    // ──────────────────────────────────────────────

    [Test]
    public void String_Empty_Large_Unicode()
    {
        var empty = "";
        var unicode = "こんにちは世界 🌍 🚀";
        var large = new string('A', 100_000);

        TestPrimitiveLifecycle(empty, unicode, large);
    }

    [Test]
    public void ArrayTypes_ReferenceSwapping()
    {
        var arr1 = new byte[] { 1, 2, 3 };
        var arr2 = new byte[] { 4, 5, 6 };
        var arr3 = new byte[] { 7, 8, 9 };

        var buffer = new DoubleBuffer<byte[]>(arr1, arr2, DoubleBufferSwapEffect.FlipRefOrValue);

        Assert.That(buffer.ReadFrontBuffer(), Is.SameAs(arr1));
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.SameAs(arr2));

        buffer.BackWriter.UpdateBackBuffer(arr3);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.SameAs(arr3));
    }

    [Test]
    public void Collections_List_And_Dictionary()
    {
        var list1 = new List<int> { 1, 2 };
        var list2 = new List<int> { 3, 4 };
        var bufferList = new DoubleBuffer<List<int>>(list1, list2, DoubleBufferSwapEffect.FlipRefOrValue);

        Assert.That(bufferList.ReadFrontBuffer(), Is.SameAs(list1));
        bufferList.SwapBuffers();
        Assert.That(bufferList.ReadFrontBuffer(), Is.SameAs(list2));

        var dict1 = new Dictionary<string, int> { ["a"] = 1 };
        var dict2 = new Dictionary<string, int> { ["b"] = 2 };
        var bufferDict = new DoubleBuffer<Dictionary<string, int>>(dict1, dict2, DoubleBufferSwapEffect.FlipRefOrValue);

        Assert.That(bufferDict.ReadFrontBuffer(), Is.SameAs(dict1));
        bufferDict.SwapBuffers();
        Assert.That(bufferDict.ReadFrontBuffer(), Is.SameAs(dict2));
    }

    [Test]
    public void RecordClass_And_RecordStruct()
    {
        var rc1 = new RecordPayload(1, "A", 1.5);
        var rc2 = new RecordPayload(2, "B", 2.5);
        var bufferRc = new DoubleBuffer<RecordPayload>(rc1, rc2, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(bufferRc.ReadFrontBuffer(), Is.EqualTo(rc1));
        bufferRc.SwapBuffers();
        Assert.That(bufferRc.ReadFrontBuffer(), Is.EqualTo(rc2));

        var rs1 = new RecordStructPayload(1, 10.0);
        var rs2 = new RecordStructPayload(2, 20.0);
        var bufferRs = new DoubleBuffer<RecordStructPayload>(rs1, rs2, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(bufferRs.ReadFrontBuffer(), Is.EqualTo(rs1));
        bufferRs.SwapBuffers();
        Assert.That(bufferRs.ReadFrontBuffer(), Is.EqualTo(rs2));
    }

    [Test]
    public void Tuples_ValueTuple_And_ReferenceTuple()
    {
        var vt1 = (1, "A", true);
        var vt2 = (2, "B", false);
        var bufferVt = new DoubleBuffer<(int, string, bool)>(vt1, vt2, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(bufferVt.ReadFrontBuffer(), Is.EqualTo(vt1));
        bufferVt.SwapBuffers();
        Assert.That(bufferVt.ReadFrontBuffer(), Is.EqualTo(vt2));

        var t1 = Tuple.Create(1, "A");
        var t2 = Tuple.Create(2, "B");
        var bufferT = new DoubleBuffer<Tuple<int, string>>(t1, t2, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(bufferT.ReadFrontBuffer(), Is.SameAs(t1));
        bufferT.SwapBuffers();
        Assert.That(bufferT.ReadFrontBuffer(), Is.SameAs(t2));
    }

    private static void TestPrimitiveLifecycle<T>(T front0, T back0, T update)
    {
        var buffer = new DoubleBuffer<T>(front0, back0, DoubleBufferSwapEffect.FlipRefOrValue);
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(front0));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(back0));

        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(back0));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(front0));

        buffer.BackWriter.UpdateBackBuffer(update);
        buffer.SwapBuffers();
        Assert.That(buffer.ReadFrontBuffer(), Is.EqualTo(update));
        Assert.That(buffer.ReadBackBuffer(), Is.EqualTo(back0));
    }
}
