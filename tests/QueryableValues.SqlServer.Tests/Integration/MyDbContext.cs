#if TESTS
namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
#if EFCORE3
    public class MyDbContext : MyDbContextBase
    {
        public MyDbContext() : base("QueryableValuesTestsEFCore3") { }
    }
#elif EFCORE5
    public class MyDbContext : MyDbContextBase
    {
        public MyDbContext() : base("QueryableValuesTestsEFCore5") { }
    }
#elif EFCORE6
    public class MyDbContext : MyDbContextBase
    {
        public MyDbContext() : base("QueryableValuesTestsEFCore6") { }
    }
#endif
}
#endif