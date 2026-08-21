// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Benchmarking;

BenchmarkRunner.Run<Benchmarks>();

namespace Benchmarking
{
    public class Benchmarks
    {
        private const int Len = 100_000;
        private Random _rand = new();
        private Consumer _c = new();

        [Benchmark]
        public void HeapWriting()
        {
            var heap = new int[Len];
            
            for (var i = 0; i < heap.Length; i++)
                heap[i] = _rand.Next();
        }

        [Benchmark]
        public void HeapReading()
        {
            var heap = new int[Len];
            
            for (var i = 0; i < heap.Length; i++)
                _c.Consume(heap[i]);
        }

        [Benchmark]
        public void StackWriting()
        {
            Span<int> stack = stackalloc int[Len];
        
            for (var i = 0; i < stack.Length; i++)
                stack[i] = _rand.Next();
        }

        [Benchmark]
        public unsafe void StackReading()
        {
            ReadOnlySpan<int> stack = stackalloc int[Len];
        
            for (var i = 0; i < stack.Length; i++)
                _c.Consume(stack[i]);
        }
    }
}