#if EFCORE
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace BlazarTech.QueryableValues.SqlServer
{
    sealed class JsonSupportConnectionInterceptor : DbConnectionInterceptor
    {
        private static readonly ConcurrentDictionary<string, bool> ConnectionStringJsonSupport = new();

        private readonly ILogger _logger;

        public JsonSupportConnectionInterceptor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(new DbLoggerCategory.Database.Command());
        }

        private static string GetKey(DbConnection connection)
        {
            return connection.ConnectionString;
        }

        public static bool HasJsonSupport(DbContext dbContext)
        {
            var connection = dbContext.Database.GetDbConnection();

            if (ConnectionStringJsonSupport.TryGetValue(GetKey(connection), out var hasJsonSupport))
            {
                return hasJsonSupport;
            }

            return false;
        }

        private static bool MustDetect(DbConnection connection)
        {
            return !ConnectionStringJsonSupport.ContainsKey(GetKey(connection));
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (connection is SqlConnection sqlConnection && MustDetect(sqlConnection))
            {
                await DetectJsonSupportAsync(sqlConnection).ConfigureAwait(false);
            }
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (connection is SqlConnection sqlConnection && MustDetect(sqlConnection))
            {
                DetectJsonSupportAsync(sqlConnection)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private async ValueTask DetectJsonSupportAsync(SqlConnection connection)
        {
            var hasJsonSupport = false;

            try
            {
                var majorVersionNumber = getMajorVersionNumber(connection.ServerVersion);

                // https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql
                if (majorVersionNumber >= 13)
                {
                    using var cm = new SqlCommand("SELECT [compatibility_level] FROM [sys].[databases] WHERE [database_id] = DB_ID()", connection);
                    var compatibilityLevelObject = await cm.ExecuteScalarAsync().ConfigureAwait(false);
                    var compatibilityLevel = Convert.ToInt32(compatibilityLevelObject);
                    hasJsonSupport = compatibilityLevel >= 130;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }

            ConnectionStringJsonSupport.TryAdd(GetKey(connection), hasJsonSupport);

            static int getMajorVersionNumber(string? serverVersion)
            {
                if (Version.TryParse(serverVersion, out var version))
                {
                    return version.Major;
                }

                return 0;
            }
        }
    }
}
#endif