using BenchmarkDotNet.Running;

namespace QueryableValues.SqlServer.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ContainsBenchmarks>();
    }
}