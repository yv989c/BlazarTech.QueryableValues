#if EFCORE
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal sealed class QueryableValuesSqlServerExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);
        public QueryableValuesSqlServerOptions Options { get; } = new QueryableValuesSqlServerOptions();

        public void ApplyServices(IServiceCollection services)
        {
            services.AddQueryableValuesSqlServer();
        }

        public void Validate(IDbContextOptions options)
        {
        }

        private class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private const string EntensionName = "BlazarTech.QueryableValues.SqlServer";

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => EntensionName;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }

#if EFCORE3 || EFCORE5
            public override long GetServiceProviderHashCode() => 0;
#else
            public override int GetServiceProviderHashCode() => 0;
            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#endif
        }
    }
}
#endif