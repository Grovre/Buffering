using System.Runtime.InteropServices;
using Buffering.Parallel;
using NUnit.Framework;

namespace Testing;

[TestFixture]
public class ParallelTests
{
    [Test]
    public void EvenPartitioning()
    {
        var arr = Enumerable.Range(0, 100).ToArray();
        Span<Range> ranges = stackalloc Range[10];
        arr.AsSpan().Partition(10, ranges);

        Assert.That(ranges[0].Start.Value == 0 && ranges[0].End.Value == 10);
        Assert.That(ranges[1].Start.Value == 10 && ranges[1].End.Value == 20);
        Assert.That(ranges[2].Start.Value == 20 && ranges[2].End.Value == 30);
        Assert.That(ranges[3].Start.Value == 30 && ranges[3].End.Value == 40);
        Assert.That(ranges[4].Start.Value == 40 && ranges[4].End.Value == 50);
        Assert.That(ranges[5].Start.Value == 50 && ranges[5].End.Value == 60);
        Assert.That(ranges[6].Start.Value == 60 && ranges[6].End.Value == 70);
        Assert.That(ranges[7].Start.Value == 70 && ranges[7].End.Value == 80);
        Assert.That(ranges[8].Start.Value == 80 && ranges[8].End.Value == 90);
        Assert.That(ranges[9].Start.Value == 90 && ranges[9].End.Value == 100);
        
        Span<Range> ranges2 = stackalloc Range[10];
        arr.Partition(10, ranges2);
        
        Assert.That(ranges.SequenceEqual(ranges2));
    }

    [Test]
    public void OddPartitioning()
    {
        var arr = Enumerable.Range(0, 27).ToArray();
        Span<Range> ranges = stackalloc Range[4];
        arr.AsSpan().Partition(4, ranges);
        
        Assert.That(ranges[0].Start.Value == 0 && ranges[0].End.Value == 7);
        Assert.That(ranges[1].Start.Value == 7 && ranges[1].End.Value == 14);
        Assert.That(ranges[2].Start.Value == 14 && ranges[2].End.Value == 21);
        Assert.That(ranges[3].Start.Value == 21 && ranges[3].End.Value == 27);
        
        Span<Range> ranges2 = stackalloc Range[4];
        arr.Partition(4, ranges2);
        
        Assert.That(ranges.SequenceEqual(ranges2));
        ranges2 = arr.Partition(4);
        Assert.That(ranges.SequenceEqual(ranges2));
    }

    [Test]
    public void IncrementingPartitioning()
    {
        // List to reuse memory
        const int MaxLength = 10_000;
        var list = new List<int>(MaxLength);
        var chunks = new Range[12];
        for (var i = 1; i <= MaxLength; i++)
        {
            Array.Fill(chunks, new Range(99999, 99999 + 1)); // Should become valid or 0
            list.Clear();
            list.AddRange(Enumerable.Repeat(-1, i));

            var listSpan = CollectionsMarshal.AsSpan(list);
            if (listSpan.Length < chunks.Length)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var span = CollectionsMarshal.AsSpan(list);
                    span.Partition(chunks.Length, chunks);
                });

                continue;
            }
            
            listSpan.Partition(chunks.Length, chunks);
            Assert.That(chunks.All(c1 => chunks.All(c2 =>
            {
                if (c1.Equals(c2))
                    return true;
                
                var a = c1.Start.Value;
                var b = c1.End.Value;
                var x = c2.Start.Value;
                var y = c2.End.Value;

                return (a < x && b <= x) ||
                       (x < a && y <= a);
            })));

            Parallel.ForEach(chunks, r =>
            {
                var span = CollectionsMarshal.AsSpan(list); // Can't capture ref structs
                Assert.That(0 <= r.Start.Value && r.Start.Value < span.Length);
                Assert.That(0 < r.End.Value && r.End.Value <= span.Length);
                Assert.That(r.Start.Value < r.End.Value);
                span[r].Fill(1);
            });
            
            Assert.That(list.TrueForAll(v => v == 1));
        }
    }

    [Test]
    public void ParallelizerMoreChunksThanThreads()
    {
        var data = new IntData[1_000_000];
        foreach (ref var d in data.AsSpan())
            d = new();
        
        var pp = new PartitionParallelizer<IntData>(
            10, 
            5,
            data);
        pp.DatumHandler += (ref IntData d) => d.N = 100;
        pp.Start();
        
        pp.Join();
        Assert.That(data.All(d => d.N == 100));
    }
    
    [Test]
    public void ParallelizerMoreThreadsThanChunks()
    {
        var data = new IntData[1_000_000];
        foreach (ref var d in data.AsSpan())
            d = new();
        
        var pp = new PartitionParallelizer<IntData>(
            5, 
            10,
            data);
        pp.DatumHandler += (ref IntData d) => d.N = 100;
        pp.Start();
        
        pp.Join();
        Assert.That(data.All(d => d.N == 100));
    }
    
    [Test]
    public void ParallelizerEqualChunksAndThreads()
    {
        var data = new IntData[1_000_000];
        foreach (ref var d in data.AsSpan())
            d = new();
        
        var pp = new PartitionParallelizer<IntData>(
            8, 
            8,
            data);
        pp.DatumHandler += (ref IntData d) => d.N = 100;
        pp.Start();
        
        pp.Join();
        Assert.That(data.All(d => d.N == 100));
    }

    private class IntData
    {
        public int N { get; set; } = -1;
    }
}