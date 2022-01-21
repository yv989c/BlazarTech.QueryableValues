using BenchmarkDotNet.Running;

namespace QueryableValues.SqlServer.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ContainsBenchmarks>();
        //var summary = BenchmarkRunner.Run<XmlBenchmarks>();
        //Test();
    }

    static void Test()
    {
        var asd = new ContainsBenchmarks();
        asd.GlobalSetup();
        asd.NumberOfValues = 4096;

        //asd.Without_Int32();
        asd.With_Int32();

        //for (int i = 0; i < 5000; i++)
        //{
        //    asd.Without_Int32();
        //    //asd.With_Int32();
        //}

        for (int i = 0; i < 5000; i++)
        {
            //asd.Without_Int32();
            asd.With_Int32();
        }
    }
}