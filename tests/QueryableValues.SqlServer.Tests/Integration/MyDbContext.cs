#if TESTS
using Microsoft.EntityFrameworkCore;

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

    public class MyBadDbContext : MyDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("BadConnectionString");
        }
    }
}
#endif