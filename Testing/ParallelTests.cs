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
    }
}