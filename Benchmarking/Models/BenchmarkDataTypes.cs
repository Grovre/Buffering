namespace Benchmarking.Models;

/// <summary>
/// A 24-byte struct representing a 3D coordinate vector to test value types larger than 64-bit primitives.
/// </summary>
public struct Vector3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Vector3D()
    {
        X = 1.0;
        Y = 2.0;
        Z = 3.0;
    }

    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>
/// A reference type payload to test heap object pointer handling and zero-allocation object recycling.
/// </summary>
public class PayloadClass
{
    public int Id { get; set; } = 42;
    public string Name { get; set; } = "BenchmarkPayload";
    public double Timestamp { get; set; } = 1234567.89;
}
