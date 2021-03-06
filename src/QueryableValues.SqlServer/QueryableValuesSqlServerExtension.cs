#if EFCORE
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal sealed class QueryableValuesSqlServerExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);
        public QueryableValuesSqlServerOptions Options { get; } = new QueryableValuesSqlServerOptions();

        public void ApplyServices(IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            for (var index = services.Count - 1; index >= 0; index--)
            {
                var descriptor = services[index];
                if (descriptor.ServiceType != typeof(IModelCustomizer))
                {
                    continue;
                }

                if (descriptor.ImplementationType is null)
                {
                    continue;
                }

                // Replace theirs with ours.
                services[index] = new ServiceDescriptor(
                    descriptor.ServiceType,
                    typeof(ModelCustomizer<>).MakeGenericType(descriptor.ImplementationType),
                    descriptor.Lifetime
                );

                // Add theirs as is, so we can inject it into ours.
                services.Add(
                    new ServiceDescriptor(
                        descriptor.ImplementationType,
                        descriptor.ImplementationType,
                        descriptor.Lifetime
                    )
                );
            }

            services.AddSingleton<Serializers.IXmlSerializer, Serializers.XmlSerializer>();

            services.AddScoped<IQueryableFactory>(sp =>
            {
                var options = sp.GetRequiredService<IDbContextOptions>();
                var extension = options.FindExtension<QueryableValuesSqlServerExtension>() ?? throw new InvalidOperationException();
                var xmlSerializer = sp.GetRequiredService<Serializers.IXmlSerializer>();
                return new SqlServer.XmlQueryableFactory(xmlSerializer, extension.Options);
            });
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

#if EFCORE6
            public override int GetServiceProviderHashCode() => 0;
            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#else
            public override long GetServiceProviderHashCode() => 0;
#endif
        }
    }
}
#endif