using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace QueryableValues.SqlServer.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = new ManualConfig();

        config.Add(DefaultConfig.Instance);
        config.WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<ContainsBenchmarks>(config);
    }
}