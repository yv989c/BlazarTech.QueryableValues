using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BlazarTech.QueryableValues;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QueryableValues.SqlServer.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, targetCount: 25, invocationCount: 200)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[GcServer(true), MemoryDiagnoser]
public class ContainsBenchmarks
{
#pragma warning disable CS8618
    private IQueryable<Int32Entity> _int32Query;
    private IQueryable<GuidEntity> _guidQuery;
    private IQueryable<Int32Entity> _queryableValuesInt32Query;
    private IQueryable<GuidEntity> _queryableValuesGuidQuery;
#pragma warning restore CS8618

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

    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine("Initializing...");

        var dbContext = new MyDbContext();

        #region Init db
        {
            var wasCreated = dbContext.Database.EnsureCreated();

            if (wasCreated)
            {
                for (int i = 0; i < 1000; i++)
                {
                    dbContext.Add(new Int32Entity());
                    dbContext.Add(new GuidEntity());
                }

                dbContext.SaveChanges();
            }

            var versionParam = new SqlParameter("@Version", System.Data.SqlDbType.NVarChar, -1)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            dbContext.Database.ExecuteSqlRaw("SET @Version = @@VERSION;", versionParam);

            Console.WriteLine(versionParam.Value);

            dbContext.Database.ExecuteSqlRaw("DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;");
        }
        #endregion

        #region Int32 Queries
        {
            var intValues = GetIntValues();

            _int32Query = dbContext.Int32Entities
                .Where(i => intValues.Contains(i.Id));

            _queryableValuesInt32Query = dbContext.Int32Entities
                .Where(i => dbContext.AsQueryableValues(intValues).Contains(i.Id));
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

    [Benchmark(Baseline = true), BenchmarkCategory("Int32")]
    public void Without_Int32()
    {
        _int32Query.Any();
    }

    [Benchmark, BenchmarkCategory("Int32")]
    public void With_Int32()
    {
        _queryableValuesInt32Query.Any();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Guid")]
    public void Without_Guid()
    {
        _guidQuery.Any();
    }

    [Benchmark, BenchmarkCategory("Guid")]
    public void With_Guid()
    {
        _queryableValuesGuidQuery.Any();
    }
}
