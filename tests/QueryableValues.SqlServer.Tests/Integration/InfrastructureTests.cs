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

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                _ = await db.AsQueryableValues(values).ToListAsync();
            });

            Assert.StartsWith($"{nameof(QueryableValues)} have not been configured", actualException?.Message);
        }

        [Fact]
        public void MustNotScriptOutInternalEntity()
        {
            using var db = new MyDbContext();

            var script = db.Database.GenerateCreateScript();

            Assert.DoesNotContain(nameof(QueryableValuesEntity<object>), script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(nameof(QueryableValuesEntity), script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("QueryableValues", script, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif