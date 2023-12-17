using System.Diagnostics;

namespace Buffering.Parallel;

/// <summary>
/// Provides extensions for partitioning an array or span easily
/// </summary>
public static class PartitionExtensions
{
    /// <summary>
    /// Outputs ranges representing a partitioned span
    /// </summary>
    /// <param name="span">The input span</param>
    /// <param name="chunks">Amount of chunks/partitions</param>
    /// <param name="dst">Where to output the ranges</param>
    /// <typeparam name="T">Type of span</typeparam>
    public static void Partition<T>(this ReadOnlySpan<T> span, int chunks, Span<Range> dst)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunks);
        ArgumentOutOfRangeException.ThrowIfLessThan(dst.Length, chunks);
        
        var chunkSize = (int)MathF.Ceiling((float)span.Length / chunks);
        chunks -= 1;
        for (var i = 0; i < chunks; i++)
        {
            var chunkIndex = i * chunkSize;
            var range = chunkIndex..(chunkIndex + chunkSize);
            Debug.Assert(chunkIndex < span.Length && range.End.Value <= span.Length);

            dst[i] = range;
        }

        dst[chunks] = (chunks * chunkSize)..span.Length;
    }

    /// <inheritdoc cref="Partition{T}(System.ReadOnlySpan{T},int,System.Span{System.Range})"/>
    public static void Partition<T>(this Span<T> span, int chunks, Span<Range> dst)
        => ((ReadOnlySpan<T>)span).Partition(chunks, dst);

    /// <summary>
    /// Outputs ranges representing a partitioned span
    /// </summary>
    /// <param name="span">The input span to partition</param>
    /// <param name="chunks">Amount of chunks/partitions to create</param>
    /// <typeparam name="T">Type of span</typeparam>
    /// <returns>A new array containing all partition ranges</returns>
    public static Range[] Partition<T>(this ReadOnlySpan<T> span, int chunks)
    {
        var ranges = new Range[chunks];
        span.Partition(chunks, ranges);
        return ranges;
    }
    
    /// <inheritdoc cref="Partition{T}(System.ReadOnlySpan{T},int)"/>
    public static Range[] Partition<T>(this Span<T> span, int chunks)
    => ((ReadOnlySpan<T>)span).Partition(chunks);

    /// <summary>
    /// Outputs ranges representing a partitioned array
    /// </summary>
    /// <param name="array">Array to partition</param>
    /// <param name="chunks">Amount of chunks.partitions to create</param>
    /// <param name="dst">Where to output the partition ranges</param>
    /// <typeparam name="T">Type of array</typeparam>
    public static void Partition<T>(this T[] array, int chunks, Span<Range> dst)
        => array.AsSpan().Partition(chunks, dst);

    /// <summary>
    /// Outputs ranges representing a partitioned array
    /// </summary>
    /// <param name="array">Array to partition</param>
    /// <param name="chunks">Amount of chunks.partitions to create</param>
    /// <typeparam name="T">Type of array</typeparam>
    /// <returns>A new array containing all partition ranges</returns>
    public static Range[] Partition<T>(this T[] array, int chunks)
        => array.AsSpan().Partition(chunks);
}