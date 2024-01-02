// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Buffering.Recycling;

var pool = new ObjectPool<Data<Int128>>(() => new Data<Int128>(1_000_000), 10);
var data = new Data<Int128>[pool.Capacity * 10];

// All data will contain 1,000,000 as initial value since pool has nothing yet
pool.TakeRange(data);
foreach (var o in data)
    Console.WriteLine(o);

// First 10 data will contain value of 0 because pool is full
pool.EnsurePoolContainsAtLeast(pool.Capacity);
pool.TakeRange(data.AsSpan(..pool.Capacity));
Console.WriteLine();
foreach (var o in data.Take(..pool.Capacity))
    Console.WriteLine(o);

pool.TryPutRange(data);
// Never use data array again for ownership semantics

class Data<T>(T number) : IRecyclable
    where T : INumber<T>
{
    public T Number { get; private set; } = number;

    public void Reset()
    {
        Number = T.Zero;
    }

    public override string? ToString()
    {
        return Number.ToString("N0", null);
    }
}