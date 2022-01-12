using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BlazarTech.QueryableValues;
using Microsoft.EntityFrameworkCore;

namespace QueryableValues.SqlServer.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, invocationCount: 200, id: nameof(ContainsBenchmarks))]
//[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 5, targetCount: 100, invocationCount: 5)]
//[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 5, targetCount: 1000, invocationCount: 5)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[MemoryDiagnoser]
public class ContainsBenchmarks
{
#pragma warning disable CS8618
    private IQueryable<IntEntity> _classicIntQuery;
    private IQueryable<GuidEntity> _classicGuidQuery;
    private IQueryable<IntEntity> _queryableValuesIntQuery;
    private IQueryable<GuidEntity> _queryableValuesGuidQuery;
#pragma warning restore CS8618

    //[Params(5, 10, 50, 100, 150, 200, 250, 500, 750, 1000)]
    //[Params(8, 16, 32, 64, 128, 256, 512)]
    [Params(32)]
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
                    dbContext.Add(new IntEntity());
                    dbContext.Add(new GuidEntity());
                }

                dbContext.SaveChanges();
            }

            dbContext.Database.ExecuteSqlRaw("DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;");
        }
        #endregion

        #region Int Queries
        {
            var intValues = GetIntValues();

            _classicIntQuery = dbContext.IntEntities
                .Where(i => intValues.Contains(i.Id));

            _queryableValuesIntQuery = dbContext.IntEntities
                .Where(i => dbContext.AsQueryableValues(intValues).Contains(i.Id));
        }
        #endregion

        #region Guid Queries
        {
            var guidValues = GetGuidValues();

            _classicGuidQuery = dbContext.GuidEntities
                .Where(i => guidValues.Contains(i.Id));

            _queryableValuesGuidQuery = dbContext.GuidEntities
                .Where(i => dbContext.AsQueryableValues(guidValues).Contains(i.Id));
        }
        #endregion
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Int32")]
    public void Classic_Int32()
    {
        _classicIntQuery.Any();
    }

    [Benchmark, BenchmarkCategory("Int32")]
    public void QueryableValues_Int32()
    {
        _queryableValuesIntQuery.Any();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Guid")]
    public void Classic_Guid()
    {
        _classicGuidQuery.Any();
    }

    [Benchmark, BenchmarkCategory("Guid")]
    public void QueryableValues_Guid()
    {
        _queryableValuesGuidQuery.Any();
    }
}
