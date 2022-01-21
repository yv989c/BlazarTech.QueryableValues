using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BlazarTech.QueryableValues;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QueryableValues.SqlServer.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, targetCount: 10, invocationCount: 200)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[GcServer(true), MemoryDiagnoser]
public class ContainsBenchmarks
{
#pragma warning disable CS8618
    private IQueryable<Int32Entity> _int32Query;
    private IQueryable<GuidEntity> _guidQuery;
    private IQueryable<Int32Entity> _queryableValuesInt32Query;
    private IQueryable<Int32Entity> _queryableValuesInt32Query2;
    private IQueryable<GuidEntity> _queryableValuesGuidQuery;
#pragma warning restore CS8618

    private MyDbContext _myDbContext;

    //[Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096)]
    //[Params(2, 8, 32, 128)]
    [Params(4096)]
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

    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine("Initializing...");

        var dbContext = new MyDbContext();
        _myDbContext = dbContext;

        //#region Init db
        //{
        //    var wasCreated = dbContext.Database.EnsureCreated();

        //    if (wasCreated)
        //    {
        //        for (int i = 0; i < 1000; i++)
        //        {
        //            dbContext.Add(new Int32Entity());
        //            dbContext.Add(new GuidEntity());
        //        }

        //        dbContext.SaveChanges();
        //    }

        //    var versionParam = new SqlParameter("@Version", System.Data.SqlDbType.NVarChar, -1)
        //    {
        //        Direction = System.Data.ParameterDirection.Output
        //    };

        //    dbContext.Database.ExecuteSqlRaw("SET @Version = @@VERSION;", versionParam);

        //    Console.WriteLine(versionParam.Value);

        //    dbContext.Database.ExecuteSqlRaw("DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;");
        //}
        //#endregion

        #region Int32 Queries
        {
            var intValues = GetIntValues();

            _int32Query = dbContext.Int32Entities
                .Where(i => intValues.Contains(i.Id));

            _queryableValuesInt32Query = dbContext.Int32Entities
                .Where(i => dbContext.AsQueryableValues(intValues).Contains(i.Id));

            var intValues2 = GetIntValues().ToList();
            _queryableValuesInt32Query2 = dbContext.Int32Entities
                .Where(i => dbContext.AsQueryableValues(intValues2).Contains(i.Id));
            //_queryableValuesInt32Query = dbContext.AsQueryableValues(intValues);
        }
        #endregion

        #region Guid Queries
        {
            var guidValues = GetGuidValues();

            _guidQuery = dbContext.GuidEntities
                .Where(i => guidValues.Contains(i.Id));

            _queryableValuesGuidQuery = dbContext.GuidEntities
                .Where(i => dbContext.AsQueryableValues(guidValues).Contains(i.Id));
        }
        #endregion
    }

    //[Benchmark(Baseline = true), BenchmarkCategory("Int32")]
    //public void Without_Int32()
    //{
    //    _int32Query.Any();
    //}

    [Benchmark(Baseline = true), BenchmarkCategory("Int32")]
    public void With_Int32()
    {
        _queryableValuesInt32Query.Any();
    }

    [Benchmark, BenchmarkCategory("Int32")]
    public void With_Int32_2()
    {
        _queryableValuesInt32Query2.Any();
    }

    [Benchmark, BenchmarkCategory("Int32")]
    public void With_Int32_Command()
    {
        using var cn = new SqlConnection(_myDbContext.Database.GetConnectionString());
        using var cm = cn.CreateCommand();
        cm.CommandText = @"
SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM [dbo].[Int32Entities] AS [i]
        WHERE EXISTS (
            SELECT 1
            FROM (
                SELECT I.value('. cast as xs:integer?', 'int') AS V FROM @p0.nodes('/R/V') N(I)
            ) AS [b]
            WHERE [b].[V] = [i].[Id])) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
";
        var p0 = new SqlParameter("@p0", System.Data.SqlDbType.Xml)
        {
            Value = XmlUtil.GetXml(GetIntValues())
        };

        cm.Parameters.Add(p0);

        cn.Open();
        var result = cm.ExecuteScalar();
        cn.Close();
    }

    [Benchmark, BenchmarkCategory("Int32")]
    public void With_Int32_Command2()
    {
        using var cn = new SqlConnection(_myDbContext.Database.GetConnectionString());
        using var cm = cn.CreateCommand();
        cm.CommandText = @"
SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM [dbo].[Int32Entities] AS [i]
        WHERE EXISTS (
            SELECT 1
            FROM (
                SELECT I.value('. cast as xs:integer?', 'int') AS V FROM @p0.nodes('/R/V') N(I)
            ) AS [b]
            WHERE [b].[V] = [i].[Id])) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
";
        var p0 = new SqlParameter("@p0", System.Data.SqlDbType.Xml)
        {
            Value = NewXmlUtil.GetXml(GetIntValues())
        };

        cm.Parameters.Add(p0);

        cn.Open();
        var result = cm.ExecuteScalar();
        cn.Close();
    }

    //[Benchmark(Baseline = true), BenchmarkCategory("Guid")]
    //public void Without_Guid()
    //{
    //    _guidQuery.Any();
    //}

    //[Benchmark, BenchmarkCategory("Guid")]
    //public void With_Guid()
    //{
    //    _queryableValuesGuidQuery.Any();
    //}
}
