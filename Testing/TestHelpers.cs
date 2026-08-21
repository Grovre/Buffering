using System;
using System.Collections.Generic;

namespace Testing;

/// <summary>
/// A mutable reference type with an ID and value for testing reference-type semantics.
/// </summary>
public sealed class TestObject : IEquatable<TestObject>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
    public byte[]? Payload { get; set; }

    public TestObject(int id, string name = "", int value = 0)
    {
        Id = id;
        Name = name;
        Value = value;
    }

    public bool Equals(TestObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name && Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as TestObject);

    public override int GetHashCode() => HashCode.Combine(Id, Name, Value);

    public override string ToString() => $"TestObject(Id={Id}, Name='{Name}', Value={Value})";
}

/// <summary>
/// Frame data mimicking the README example for zero-allocation recycling.
/// </summary>
public sealed class FrameData
{
    public byte[] Pixels { get; }
    public long Timestamp { get; set; }
    public int FrameIndex { get; set; }

    public FrameData(int pixelCount = 1024)
    {
        Pixels = new byte[pixelCount];
    }
}

/// <summary>
/// World state struct mimicking the README example for incremental state accumulation.
/// </summary>
public struct WorldState : IEquatable<WorldState>
{
    public int EntityCount;
    public float SimulationTime;
    public long Tick;

    public WorldState(int entityCount, float simulationTime, long tick)
    {
        EntityCount = entityCount;
        SimulationTime = simulationTime;
        Tick = tick;
    }

    public bool Equals(WorldState other) =>
        EntityCount == other.EntityCount &&
        SimulationTime.Equals(other.SimulationTime) &&
        Tick == other.Tick;

    public override bool Equals(object? obj) => obj is WorldState other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(EntityCount, SimulationTime, Tick);
}

/// <summary>
/// Market snapshot data representing financial telemetry data.
/// </summary>
public sealed class MarketSnapshot : IEquatable<MarketSnapshot>
{
    public string Ticker { get; }
    public decimal Bid { get; }
    public decimal Ask { get; }
    public long Volume { get; }
    public long Timestamp { get; }

    public MarketSnapshot(string ticker, decimal bid, decimal ask, long volume, long timestamp)
    {
        Ticker = ticker;
        Bid = bid;
        Ask = ask;
        Volume = volume;
        Timestamp = timestamp;
    }

    public bool Equals(MarketSnapshot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Ticker == other.Ticker &&
               Bid == other.Bid &&
               Ask == other.Ask &&
               Volume == other.Volume &&
               Timestamp == other.Timestamp;
    }

    public override bool Equals(object? obj) => Equals(obj as MarketSnapshot);

    public override int GetHashCode() => HashCode.Combine(Ticker, Bid, Ask, Volume, Timestamp);
}

/// <summary>
/// 1-byte struct.
/// </summary>
public readonly struct Struct1B : IEquatable<Struct1B>
{
    public readonly byte Value;
    public Struct1B(byte val) => Value = val;
    public bool Equals(Struct1B other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Struct1B other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>
/// 4-byte struct.
/// </summary>
public readonly struct Struct4B : IEquatable<Struct4B>
{
    public readonly int Value;
    public Struct4B(int val) => Value = val;
    public bool Equals(Struct4B other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Struct4B other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>
/// 8-byte struct.
/// </summary>
public readonly struct Struct8B : IEquatable<Struct8B>
{
    public readonly long Value;
    public Struct8B(long val) => Value = val;
    public bool Equals(Struct8B other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Struct8B other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>
/// 16-byte struct.
/// </summary>
public readonly struct Struct16B : IEquatable<Struct16B>
{
    public readonly long A;
    public readonly long B;

    public Struct16B(long val)
    {
        A = val;
        B = val;
    }

    public Struct16B(long a, long b)
    {
        A = a;
        B = b;
    }

    public bool IsValid() => A == B;
    public bool Equals(Struct16B other) => A == other.A && B == other.B;
    public override bool Equals(object? obj) => obj is Struct16B other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B);
}

/// <summary>
/// 32-byte struct (4 longs).
/// </summary>
public readonly struct Struct32B : IEquatable<Struct32B>
{
    public readonly long A;
    public readonly long B;
    public readonly long C;
    public readonly long D;

    public Struct32B(long val)
    {
        A = val;
        B = val;
        C = val;
        D = val;
    }

    public Struct32B(long a, long b, long c, long d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public bool IsValid() => A == B && B == C && C == D;
    public bool Equals(Struct32B other) => A == other.A && B == other.B && C == other.C && D == other.D;
    public override bool Equals(object? obj) => obj is Struct32B other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B, C, D);
}

/// <summary>
/// 64-byte struct (8 longs).
/// </summary>
public readonly struct Struct64B : IEquatable<Struct64B>
{
    public readonly long A;
    public readonly long B;
    public readonly long C;
    public readonly long D;
    public readonly long E;
    public readonly long F;
    public readonly long G;
    public readonly long H;

    public Struct64B(long val)
    {
        A = val;
        B = val;
        C = val;
        D = val;
        E = val;
        F = val;
        G = val;
        H = val;
    }

    public bool IsValid() =>
        A == B && B == C && C == D && D == E && E == F && F == G && G == H;

    public bool Equals(Struct64B other) =>
        A == other.A && B == other.B && C == other.C && D == other.D &&
        E == other.E && F == other.F && G == other.G && H == other.H;

    public override bool Equals(object? obj) => obj is Struct64B other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(A, B, C, D, HashCode.Combine(E, F, G, H));
}

/// <summary>
/// 128-byte struct (16 longs).
/// </summary>
public readonly struct Struct128B : IEquatable<Struct128B>
{
    public readonly Struct64B Low;
    public readonly Struct64B High;

    public Struct128B(long val)
    {
        Low = new Struct64B(val);
        High = new Struct64B(val);
    }

    public bool IsValid() => Low.IsValid() && High.IsValid() && Low.A == High.A;

    public bool Equals(Struct128B other) => Low.Equals(other.Low) && High.Equals(other.High);
    public override bool Equals(object? obj) => obj is Struct128B other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Low, High);
}

/// <summary>
/// 256-byte struct (32 longs).
/// </summary>
public readonly struct Struct256B : IEquatable<Struct256B>
{
    public readonly Struct128B Low;
    public readonly Struct128B High;

    public Struct256B(long val)
    {
        Low = new Struct128B(val);
        High = new Struct128B(val);
    }

    public bool IsValid() => Low.IsValid() && High.IsValid() && Low.Low.A == High.Low.A;

    public bool Equals(Struct256B other) => Low.Equals(other.Low) && High.Equals(other.High);
    public override bool Equals(object? obj) => obj is Struct256B other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Low, High);
}

/// <summary>
/// Mutable struct for testing struct mutation semantics.
/// </summary>
public struct MutableStruct
{
    public int X;
    public int Y;

    public MutableStruct(int x, int y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Struct holding reference types.
/// </summary>
public struct StructWithReferences
{
    public string Name;
    public List<int> Numbers;

    public StructWithReferences(string name, List<int> numbers)
    {
        Name = name;
        Numbers = numbers;
    }
}

/// <summary>
/// Sample enum for enum tests.
/// </summary>
public enum SampleEnum
{
    First = 0,
    Second = 1,
    Third = 2,
    Special = 100
}

/// <summary>
/// Flags enum for bitwise flag tests.
/// </summary>
[Flags]
public enum SampleFlags : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

/// <summary>
/// Record class payload.
/// </summary>
public record class RecordPayload(int Id, string Text, double Ratio);

/// <summary>
/// Record struct payload.
/// </summary>
public readonly record struct RecordStructPayload(int Id, double Value);

/// <summary>
/// Disposable class to verify buffer lifecycle semantics.
/// </summary>
public sealed class DisposablePayload : IDisposable
{
    public int Id { get; }
    public bool IsDisposed { get; private set; }

    public DisposablePayload(int id)
    {
        Id = id;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
