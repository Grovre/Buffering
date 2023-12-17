using System.Diagnostics;

namespace Buffering.Parallel;

public static class PartitionExtensions
{
    public static void Partition<T>(this ReadOnlySpan<T> span, int chunks, Span<Range> dst)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunks);
        ArgumentOutOfRangeException.ThrowIfLessThan(dst.Length, chunks);
        
        var chunkSize = (int)MathF.Ceiling((float)span.Length / chunks);
        for (var i = 0; i < chunks - 1; i++)
        {
            var chunkIndex = i * chunkSize;
            var range = chunkIndex..(chunkIndex + chunkSize);
            Debug.Assert(chunkIndex < span.Length && range.End.Value <= span.Length);

            dst[i] = range;
        }

        dst[chunks - 1] = ((chunks - 1) * chunkSize)..span.Length;
    }

    public static void Partition<T>(this Span<T> span, int chunks, Span<Range> dst)
        => ((ReadOnlySpan<T>)span).Partition(chunks, dst);

    public static Range[] Partition<T>(this ReadOnlySpan<T> span, int chunks)
    {
        var ranges = new Range[chunks];
        span.Partition(chunks, ranges);
        return ranges;
    }
    
    public static Range[] Partition<T>(this Span<T> span, int chunks)
    => ((ReadOnlySpan<T>)span).Partition(chunks);

    public static void Partition<T>(this T[] array, int chunks, Span<Range> dst)
        => array.AsSpan().Partition(chunks, dst);

    public static Range[] Partition<T>(this T[] array, int chunks)
        => array.AsSpan().Partition(chunks);
}