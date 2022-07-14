#if TESTS
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
#endif
    }

    public class MyDbContext : MyDbContextBase
    {
        public MyDbContext() : base(DatabaseName.Name) { }
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