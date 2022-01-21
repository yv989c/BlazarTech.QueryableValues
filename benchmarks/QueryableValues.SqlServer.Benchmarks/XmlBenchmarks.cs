using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace QueryableValues.SqlServer.Benchmarks
{
    [GcServer(true), MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlBenchmarks
    {
        [Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096)]
        public int NumberOfValues { get; set; }

        private IEnumerable<int> GetIntValues()
        {
            for (var i = 0; i < NumberOfValues; i++)
            {
                yield return Random.Shared.Next(10000);
            }
        }

        private IEnumerable<Guid> GetGuidValues()
        {
            for (var i = 0; i < NumberOfValues; i++)
            {
                yield return Guid.NewGuid();
            }
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Int32")]
        public void Int32()
        {
            _ = XmlUtil.GetXml(GetIntValues());
        }

        [Benchmark, BenchmarkCategory("Int32")]
        public void Int32New()
        {
            _ = NewXmlUtil.GetXml(GetIntValues());
        }

        //[Benchmark(Baseline = true), BenchmarkCategory("Guid")]
        //public void Guid()
        //{
        //    XmlUtil.GetXml(GetGuidValues());
        //}

        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew()
        //{
        //}
    }
}
