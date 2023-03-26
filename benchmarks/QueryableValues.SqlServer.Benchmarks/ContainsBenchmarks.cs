using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BlazarTech.QueryableValues;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace QueryableValues.SqlServer.Benchmarks;

//[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 25, invocationCount: 200)]
//[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 6, invocationCount: 200)]
[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 6, invocationCount: 32)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[GcServer(true), MemoryDiagnoser]
public class ContainsBenchmarks
{
    private IQueryable<Int32Entity> _int32Query = default!;
    private IQueryable<GuidEntity> _guidQuery = default!;
    private IQueryable<StringEntity> _stringQuery = default!;

    private IQueryable<Int32Entity> _queryableValuesJsonInt32Query = default!;
    private IQueryable<GuidEntity> _queryableValuesJsonGuidQuery = default!;
    private IQueryable<StringEntity> _queryableValuesJsonStringQuery = default!;

    private IQueryable<Int32Entity> _queryableValuesXmlInt32Query = default!;
    private IQueryable<GuidEntity> _queryableValuesXmlGuidQuery = default!;
    private IQueryable<StringEntity> _queryableValuesXmlStringQuery = default!;

    public enum DataType
    {
        Int32,
        Guid,
        String
    }

    [Params(DataType.Int32, DataType.Guid, DataType.String)]
    //[Params(DataType.String)]
    public DataType Type { get; set; }

    //[Params(512)]
    //[Params(2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096)]
    [Params(2, 8, 32, 128, 512, 2048)]
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

    private IEnumerable<string> GetStringValues()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < NumberOfValues; i++)
        {
            sb.Clear();
            var length = Random.Shared.Next(0, 50);
            for (int x = 0; x < length; x++)
            {
                sb.Append((char)Random.Shared.Next(32, 126));
            }
            yield return sb.ToString();
        }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine("Initializing...");

        var dbContextXml = new MyDbContext(SerializationOptions.UseXml);
        var dbContextJson = new MyDbContext(SerializationOptions.UseJson);

        #region Init db
        {
            var wasCreated = dbContextXml.Database.EnsureCreated();

            if (wasCreated)
            {
                const int itemsCount = 1000;

                for (int i = 0; i < itemsCount; i++)
                {
                    dbContextXml.Add(new Int32Entity());
                    dbContextXml.Add(new GuidEntity());
                }

                var i2 = 0;

                foreach (var value in GetStringValues())
                {
                    i2++;

                    dbContextXml.Add(new StringEntity { Id = $"{value}{i2}" });

                    if (i2 == itemsCount)
                    {
                        break;
                    }
                }

                dbContextXml.SaveChanges();
            }

            var versionParam = new SqlParameter("@Version", System.Data.SqlDbType.NVarChar, -1)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            dbContextXml.Database.ExecuteSqlRaw("SET @Version = @@VERSION;", versionParam);

            Console.WriteLine(versionParam.Value);

            dbContextXml.Database.ExecuteSqlRaw("DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;");
        }
        #endregion

        #region Int32 Queries
        {
            var intValues = GetIntValues();

            _int32Query = dbContextXml.Int32Entities
                .Where(i => intValues.Contains(i.Id));

            _queryableValuesXmlInt32Query = dbContextXml.Int32Entities
                .Where(i => dbContextXml.AsQueryableValues(intValues).Contains(i.Id));

            _queryableValuesJsonInt32Query = dbContextJson.Int32Entities
                .Where(i => dbContextJson.AsQueryableValues(intValues).Contains(i.Id));
        }
        #endregion

        #region Guid Queries
        {
            var guidValues = GetGuidValues();

            _guidQuery = dbContextXml.GuidEntities
                .Where(i => guidValues.Contains(i.Id));

            _queryableValuesXmlGuidQuery = dbContextXml.GuidEntities
                .Where(i => dbContextXml.AsQueryableValues(guidValues).Contains(i.Id));

            _queryableValuesJsonGuidQuery = dbContextJson.GuidEntities
                .Where(i => dbContextJson.AsQueryableValues(guidValues).Contains(i.Id));
        }
        #endregion

        #region String Queries
        {
            var stringValues = GetStringValues();

            _stringQuery = dbContextXml.StringEntities
                .Where(i => stringValues.Contains(i.Id));

            _queryableValuesXmlStringQuery = dbContextXml.StringEntities
                .Where(i => dbContextXml.AsQueryableValues(stringValues, true).Contains(i.Id));

            _queryableValuesJsonStringQuery = dbContextJson.StringEntities
                .Where(i => dbContextJson.AsQueryableValues(stringValues, true).Contains(i.Id));
        }
        #endregion
    }

    [Benchmark(Baseline = true)]
    public void Without()
    {
        switch (Type)
        {
            case DataType.Int32:
                _int32Query.Any();
                break;
            case DataType.Guid:
                _guidQuery.Any();
                break;
            case DataType.String:
                _stringQuery.Any();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [Benchmark]
    public void WithXml()
    {
        switch (Type)
        {
            case DataType.Int32:
                _queryableValuesXmlInt32Query.Any();
                break;
            case DataType.Guid:
                _queryableValuesXmlGuidQuery.Any();
                break;
            case DataType.String:
                _queryableValuesXmlStringQuery.Any();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [Benchmark]
    public void WithJson()
    {
        switch (Type)
        {
            case DataType.Int32:
                _queryableValuesJsonInt32Query.Any();
                break;
            case DataType.Guid:
                _queryableValuesJsonGuidQuery.Any();
                break;
            case DataType.String:
                _queryableValuesJsonStringQuery.Any();
                break;
            default:
                throw new NotImplementedException();
        }
    }
}