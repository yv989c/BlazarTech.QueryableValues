#if TESTS && TEST_ALL
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    [Collection("DbContext")]

    public class InfrastructureTests
    {
        [Fact]
        public async Task MustFailNotConfigured()
        {
            using var db = new MyBadDbContext();

            var values = Enumerable.Range(0, 10);

            Exception? actualException = null;

            try
            {
                _ = await db.AsQueryableValues(values).ToListAsync();
            }
            catch (Exception ex)
            {
                actualException = ex;
            }

            Assert.IsType<InvalidOperationException>(actualException);
            Assert.StartsWith($"{nameof(QueryableValues)} have not been configured", actualException?.Message);
        }
    }
}
#endif