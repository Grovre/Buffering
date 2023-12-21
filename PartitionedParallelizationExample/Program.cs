// See https://aka.ms/new-console-template for more information

using Buffering.Parallel;

Console.WriteLine("Allocating/Initializing");
var data = new IntData[100_000_000];

Console.WriteLine("Parallelization");
var pp = new PartitionParallelizer<IntData>(
    chunks: 10,
    threadCount: 10,
    data);

pp.DatumHandler += (ref IntData n) => n.N = 69;

Console.WriteLine("Working");
pp.Start();
pp.Join();
Console.WriteLine($"All data is 69?: {Array.TrueForAll(data, d => d.N == 69)}");

record struct IntData(int N = -1);