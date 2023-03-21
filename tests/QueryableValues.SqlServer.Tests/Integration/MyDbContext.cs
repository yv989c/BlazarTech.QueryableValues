#if TESTS
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    internal static class DatabaseName
    {
#if EFCORE3
        public const string Name = "QueryableValuesTestsEFCore3";
#elif EFCORE5
        public const string Name = "QueryableValuesTestsEFCore5";
#elif EFCORE6
        public const string Name = "QueryableValuesTestsEFCore6";
#elif EFCORE7
        public const string Name = "QueryableValuesTestsEFCore7";
#endif
    }

    public class MyDbContext : MyDbContextBase, IMyDbContext
    {
        public QueryableValuesSqlServerOptions Options { get; }

        public MyDbContext() : base(DatabaseName.Name)
        {
            Options = this.GetService<IDbContextOptions>().FindExtension<QueryableValuesSqlServerExtension>()!.Options;
        }
    }

    public interface IMyDbContext : IQueryableValuesEnabledDbContext
    {
        QueryableValuesSqlServerOptions Options { get; }
        DbSet<TestDataEntity> TestData { get; set; }
    }

    public class NotConfiguredDbContext : MyDbContextBase
    {
        public NotConfiguredDbContext() : base(DatabaseName.Name, useQueryableValues: false) { }
    }

    public class NotOptimizedDbContext : MyDbContextBase
    {
        public NotOptimizedDbContext() : base(DatabaseName.Name, useSelectTopOptimization: false) { }
    }
}
#endif