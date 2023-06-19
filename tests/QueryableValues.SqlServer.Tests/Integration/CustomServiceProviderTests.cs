using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    public class CustomServiceProviderTests
    {
        public class DummyDbContext : DbContext
        {
            public DummyDbContext(DbContextOptions options) : base(options)
            {
            }
        }

        private static IServiceProvider BuildGoodInternalServiceProvider()
        {
            var services = new ServiceCollection()
                .AddEntityFrameworkSqlServer()
                .AddQueryableValuesSqlServer();

            return services.BuildServiceProvider();
        }

        private static IServiceProvider BuildBadInternalServiceProvider()
        {
            var services = new ServiceCollection()
                .AddEntityFrameworkSqlServer();

            return services.BuildServiceProvider();
        }

        private static string GetConnectionString()
        {
            var databaseName = $"DummyDb{Guid.NewGuid():N}";
            var databaseFilePath = Path.Combine(Path.GetTempPath(), $"{databaseName}.mdf");
            return @$"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Connection Timeout=190;Database={databaseName};AttachDbFileName={databaseFilePath}";
        }

        private static async Task CleanUpDbAsync(DbContext dbContext)
        {
            try
            {
                await dbContext.Database.EnsureDeletedAsync();
            }
            catch
            {
            }
        }

        [Fact]
        public async Task BadInternalServiceProvider()
        {
            var internalServiceProvider = BuildBadInternalServiceProvider();
            var connectionString = GetConnectionString();
            var services = new ServiceCollection();

            services.AddDbContext<DummyDbContext>(builder =>
            {
                builder
                    .UseInternalServiceProvider(internalServiceProvider)
                    .UseSqlServer(connectionString, options =>
                    {
                        options.UseQueryableValues();
                    });
            });

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<DummyDbContext>();

            try
            {
                await dbContext.Database.EnsureCreatedAsync();

                var values = new[] { 1, 2, 3 };

                var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
                {
                    await dbContext.AsQueryableValues(values).ToListAsync();
                });

                Assert.StartsWith("QueryableValues have not been configured for ", exception.Message);
            }
            finally
            {
                await CleanUpDbAsync(dbContext);
            }
        }

#if !EFCORE3
        [Theory]
        [InlineData(SqlServerSerialization.UseJson)]
        [InlineData(SqlServerSerialization.UseXml)]
        public async Task GoodInternalServiceProviderWithConfiguration(SqlServerSerialization sqlServerSerializationOption)
        {
            var internalServiceProvider = BuildGoodInternalServiceProvider();
            var connectionString = GetConnectionString();
            var services = new ServiceCollection();
            var logEntries = new List<string>();

            services.AddDbContext<DummyDbContext>(builder =>
            {
                builder
                    .UseInternalServiceProvider(internalServiceProvider)
                    .UseSqlServer(connectionString, options =>
                    {
                        options.UseQueryableValues(options =>
                        {
                            options.Serialization(sqlServerSerializationOption);
                        });
                    })
                    .LogTo(logEntry => { logEntries.Add(logEntry); }, Microsoft.Extensions.Logging.LogLevel.Information);
            });

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<DummyDbContext>();

            try
            {
                await dbContext.Database.EnsureCreatedAsync();

                var values = new[] { 1, 2, 3 };
                var valuesResult = await dbContext.AsQueryableValues(values).ToListAsync();
                Assert.Equal(values, valuesResult);

                switch (sqlServerSerializationOption)
                {
                    case SqlServerSerialization.UseJson:
                        Assert.Contains(logEntries, i => i.Contains("FROM OPENJSON(@p0)"));
                        break;
                    case SqlServerSerialization.UseXml:
                        Assert.Contains(logEntries, i => i.Contains("FROM @p0.nodes"));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            finally
            {
                await CleanUpDbAsync(dbContext);
            }
        }
#endif
    }
}