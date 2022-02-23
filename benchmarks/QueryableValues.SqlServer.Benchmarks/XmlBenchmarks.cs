using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace QueryableValues.SqlServer.Benchmarks
{
    [GcServer(true), MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlBenchmarks
    {
        private readonly Random _random = new(1);

        private byte[] _bytes;
        private short[] _int16s;
        private int[] _int32s;
        private long[] _int64s;
        private decimal[] _decimals;
        private float[] _singles;
        private double[] _doubles;
        private DateTime[] _dateTimes;
        private DateTimeOffset[] _dateTimeOffsets;
        private Guid[] _guids;
        private string[] _strings;

        //[Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096)]
        //[Params(2, 8, 32, 4096)]
        //[Params(2, 512)]
        [Params(512)]
        public int NumberOfValues { get; set; }


        [GlobalSetup]
        public void GlobalSetup()
        {
            _bytes =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => (byte)_random.Next(byte.MaxValue + 1))
                .ToArray();

            _int16s =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => (short)_random.Next(short.MinValue, short.MaxValue + 1))
                .ToArray();

            _int32s =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => (int)_random.NextInt64(int.MinValue, (long)int.MaxValue + 1))
                .ToArray();

            _int64s =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => _random.NextInt64(int.MinValue, long.MaxValue))
                .ToArray();

            _decimals =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => (decimal)_random.NextDouble() * decimal.MaxValue)
                .ToArray();

            _singles =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => (float)_random.NextDouble() * float.MaxValue)
                .ToArray();

            _doubles =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => _random.NextDouble() * double.MaxValue)
                .ToArray();

            _dateTimes =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => DateTime.Now.AddMinutes(i))
                .ToArray();

            _dateTimeOffsets =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => DateTimeOffset.Now.AddMinutes(i))
                .ToArray();

            _guids =
                Enumerable
                .Range(0, NumberOfValues)
                .Select(i => Guid.NewGuid())
                .ToArray();

            _strings = _guids.Select(i => i.ToString()).ToArray();
        }

        //[Benchmark(Baseline = true), BenchmarkCategory("Byte")]
        //public void ByteOld()
        //{
        //    _ = XmlUtil.GetXml(_bytes);
        //}

        //[Benchmark, BenchmarkCategory("Byte")]
        //public void ByteNew()
        //{
        //    _ = XmlUtil2.GetXml(_bytes);
        //}


        //[Benchmark(Baseline = true), BenchmarkCategory("Int16")]
        //public void Int16Old()
        //{
        //    _ = XmlUtil.GetXml(_int16s);
        //}

        //[Benchmark, BenchmarkCategory("Int16")]
        //public void Int16New()
        //{
        //    _ = XmlUtil2.GetXml(_int16s);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Int32")]
        //public void Int32Old()
        //{
        //    _ = XmlUtil.GetXml(_int32s);
        //}

        //[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New()
        //{
        //    _ = XmlUtil2.GetXml(_int32s);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Int64")]
        //public void Int64Old()
        //{
        //    _ = XmlUtil.GetXml(_int64s);
        //}

        //[Benchmark, BenchmarkCategory("Int64")]
        //public void Int64New()
        //{
        //    _ = XmlUtil2.GetXml(_int64s);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Decimal")]
        //public void DecimalOld()
        //{
        //    _ = XmlUtil.GetXml(_decimals);
        //}

        //[Benchmark, BenchmarkCategory("Decimal")]
        //public void DecimalNew()
        //{
        //    _ = XmlUtil2.GetXml(_decimals);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Single")]
        //public void SingleOld()
        //{
        //    _ = XmlUtil.GetXml(_singles);
        //}

        //[Benchmark, BenchmarkCategory("Single")]
        //public void SingleNew()
        //{
        //    _ = XmlUtil2.GetXml(_singles);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Double")]
        //public void DoubleOld()
        //{
        //    _ = XmlUtil.GetXml(_doubles);
        //}

        //[Benchmark, BenchmarkCategory("Double")]
        //public void DoubleNew()
        //{
        //    _ = XmlUtil2.GetXml(_doubles);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("DateTime")]
        //public void DateTimeOld()
        //{
        //    _ = XmlUtil.GetXml(_dateTimes);
        //}

        //[Benchmark, BenchmarkCategory("DateTime")]
        //public void DateTimeNew()
        //{
        //    _ = XmlUtil2.GetXml(_dateTimes);
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("DateTimeOffset")]
        //public void DateTimeOffsetOld()
        //{
        //    _ = XmlUtil.GetXml(_dateTimeOffsets);
        //}

        //[Benchmark, BenchmarkCategory("DateTimeOffset")]
        //public void DateTimeOffsetNew()
        //{
        //    _ = XmlUtil2.GetXml(_dateTimeOffsets);
        //}


        //[Benchmark(Baseline = true), BenchmarkCategory("Guid")]
        //public void GuidOld()
        //{
        //    _ = XmlUtil.GetXml(_guids);
        //}

        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew()
        //{
        //    _ = XmlUtil2.GetXml(_guids);
        //}


        [Benchmark(Baseline = true), BenchmarkCategory("String")]
        public void StringOld()
        {
            _ = XmlUtil.GetXml(_strings);
        }

        [Benchmark, BenchmarkCategory("String")]
        public void StringNew()
        {
            _ = XmlUtil2.GetXml(_strings);
        }

        // ################################

        private IEnumerable<int> GetIntValues()
        {
            return _int32s;

            //return Enumerable
            //    .Range(0, NumberOfValues)
            //    .Select(i => _random.Next(10000));

            //for (var i = 0; i < NumberOfValues; i++)
            //{
            //    yield return _random.Next(10000);
            //}
        }

        private IEnumerable<Guid> GetGuidValues()
        {
            return _guids;

            //return Enumerable
            //    .Range(0, NumberOfValues)
            //    .Select(i => System.Guid.NewGuid());

            //for (var i = 0; i < NumberOfValues; i++)
            //{
            //    yield return Guid.NewGuid();
            //}
        }

        //[Benchmark(Baseline = true), BenchmarkCategory("Int32")]
        //public void Int32()
        //{
        //    _ = XmlUtil.GetXml(GetIntValues());
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Int32")]
        ////[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New()
        //{
        //    _ = NewXmlUtil.GetXml(GetIntValues());
        //}

        ////[Benchmark, BenchmarkCategory("Int32")]
        ////public void Int32New4()
        ////{
        ////    _ = NewXmlUtil.GetXml4(GetIntValues());
        ////}

        //[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New5()
        //{
        //    _ = NewXmlUtil.GetXml5(GetIntValues());
        //}


        //[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New6()
        //{
        //    _ = NewXmlUtil.GetXml6(GetIntValues());
        //}

        //[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New2()
        //{
        //    _ = NewXmlUtil.GetXml2(GetIntValues());
        //}

        //[Benchmark, BenchmarkCategory("Int32")]
        //public void Int32New3()
        //{
        //    _ = NewXmlUtil.GetXml3(GetIntValues());
        //}

        //[Benchmark(Baseline = true), BenchmarkCategory("Guid")]
        //public void Guid()
        //{
        //    _ = XmlUtil.GetXml(GetGuidValues());
        //}

        ////[Benchmark(Baseline = true), BenchmarkCategory("Guid")]
        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew()
        //{
        //    _ = NewXmlUtil.GetXml(GetGuidValues());
        //}

        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew2()
        //{
        //    _ = NewXmlUtil.GetXmlGuid2(GetGuidValues());
        //}

        //[Benchmark, BenchmarkCategory("Byte")]
        //public void ByteNew2()
        //{
        //    _ = XmlUtil2.GetXml2(_bytes);
        //}
        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew3()
        //{
        //    _ = NewXmlUtil.GetXmlGuid3(GetGuidValues());
        //}


        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew4()
        //{
        //    _ = NewXmlUtil.GetXmlGuid4(GetGuidValues());
        //}


        //[Benchmark, BenchmarkCategory("Guid")]
        //public void GuidNew5()
        //{
        //    _ = NewXmlUtil.GetXmlGuid5(GetGuidValues());
        //}
    }
}
