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
#elif EFCORE8
        public const string Name = "QueryableValuesTestsEFCore8";
#endif
    }

    public class MyDbContext : MyDbContextBase, IMyDbContext
    {
        public QueryableValuesSqlServerOptions Options { get; }

        public MyDbContext(bool useSelectTopOptimization = true) : base(DatabaseName.Name, useSelectTopOptimization: useSelectTopOptimization)
        {
            Options = this.GetService<IDbContextOptions>().FindExtension<QueryableValuesSqlServerExtension>()!.Options;
        }
    }

    public interface IMyDbContext : IQueryableValuesEnabledDbContext
    {
        QueryableValuesSqlServerOptions Options { get; }
        DbSet<TestDataEntity> TestData { get; set; }
        DbSet<ChildEntity> ChildEntity { get; set; }
    }

    public class NotConfiguredDbContext : MyDbContextBase
    {
        public NotConfiguredDbContext() : base(DatabaseName.Name, useQueryableValues: false) { }
    }

    public class NotOptimizedMyDbContext : MyDbContext
    {
        public NotOptimizedMyDbContext() : base(useSelectTopOptimization: false) { }
    }
}
#endif