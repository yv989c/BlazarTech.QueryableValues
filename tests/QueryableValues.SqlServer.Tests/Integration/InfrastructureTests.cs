#if TESTS && TEST_ALL
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            using var db = new NotConfiguredDbContext();

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

        [Fact]
        public void OnlyWorksOnDbContext()
        {
            var db = new NotADbContext();

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                _ = db.AsQueryableValues(new[] { 1 }).ToList();
            });

            Assert.Contains("QueryableValues only works on a Microsoft.EntityFrameworkCore.DbContext type.", exception.Message);
        }

#if !EFCORE3
        [Theory]
        [InlineData(SerializationOptions.UseJson)]
        [InlineData(SerializationOptions.UseXml)]
        public async Task MustControlSelectTopOptimization(SerializationOptions serializationOptions)
        {
            var services = new ServiceCollection();
            services.AddDbContext<MyDbContext>();
            services.AddDbContext<NotOptimizedMyDbContext>();
            using var serviceProvider = services.BuildServiceProvider();

            var optimizedDb = serviceProvider.GetRequiredService<MyDbContext>();
            optimizedDb.Options.Serialization(serializationOptions);
            Assert.True(await isOptimizationEnabledSimpleType(optimizedDb));
            Assert.True(await isOptimizationEnabledComplexType(optimizedDb));

            var notOptimizedDb = serviceProvider.GetRequiredService<NotOptimizedMyDbContext>();
            notOptimizedDb.Options.Serialization(serializationOptions);
            Assert.False(await isOptimizationEnabledComplexType(notOptimizedDb));
            Assert.False(await isOptimizationEnabledSimpleType(notOptimizedDb));

            async Task<bool> isOptimizationEnabledSimpleType(MyDbContextBase db)
            {
                var values = new[] { 1, 2, 3 };
                var logEntries = new List<string>();
                db.LogEntryEmitted += logEntry => logEntries.Add(logEntry);
                var result = await db.AsQueryableValues(values).ToListAsync();
                Assert.Equal(values.Length, result.Count);
                var logEntry = logEntries.Single(i => i.Contains("RelationalEventId.CommandExecuted"));
                return Regex.IsMatch(logEntry, @"SELECT TOP\(@\w+\)\s");
            }

            async Task<bool> isOptimizationEnabledComplexType(MyDbContextBase db)
            {
                var values = new[]
                {
                    new { Id = 1 },
                    new { Id = 2 },
                    new { Id = 3 }
                };
                var logEntries = new List<string>();
                db.LogEntryEmitted += logEntry => logEntries.Add(logEntry);
                var result = await db.AsQueryableValues(values).ToListAsync();
                Assert.Equal(values.Length, result.Count);
                var logEntry = logEntries.Single(i => i.Contains("RelationalEventId.CommandExecuted"));
                return Regex.IsMatch(logEntry, @"SELECT TOP\(@\w+\)\s");
            }
        }
#endif

        [Theory]
        [InlineData(SerializationOptions.UseJson)]
        [InlineData(SerializationOptions.UseXml)]
        public void MustCreateQueryableFactory(SerializationOptions serializationOptions)
        {
            var services = new ServiceCollection();
            services.AddDbContext<MyDbContext>();

            using var serviceProvider = services.BuildServiceProvider();

            var dbContext = serviceProvider.GetRequiredService<MyDbContext>();
            dbContext.Options.Serialization(serializationOptions);

            var queryableFactory = dbContext.GetService<QueryableFactoryFactory>().Create();

            Assert.NotNull(queryableFactory);

            switch (serializationOptions)
            {
                case SerializationOptions.UseJson:
                    Assert.IsType<JsonQueryableFactory>(queryableFactory);
                    break;
                case SerializationOptions.UseXml:
                    Assert.IsType<XmlQueryableFactory>(queryableFactory);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    class NotADbContext : IQueryableValuesEnabledDbContext
    {
    }
}
#endif