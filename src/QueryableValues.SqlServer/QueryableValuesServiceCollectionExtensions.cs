#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Provides extension methods to register core QueryableValues services.
    /// </summary>
    public static class QueryableValuesServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the services required by QueryableValues for the Microsoft SQL Server database provider.
        /// </summary>
        /// <remarks>
        /// It is only needed when building the internal service provider for use with
        /// the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
        /// This is not recommend other than for some advanced scenarios.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddQueryableValuesSqlServer(this IServiceCollection services)
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

            services.TryAddSingleton<Serializers.IXmlSerializer, Serializers.XmlSerializer>();
            services.TryAddSingleton<Serializers.IJsonSerializer, Serializers.JsonSerializer>();
            services.TryAddScoped<SqlServer.XmlQueryableFactory>();
            services.TryAddScoped<SqlServer.JsonQueryableFactory>();
            services.TryAddScoped<SqlServer.ExtensionOptions>();
            services.TryAddScoped<SqlServer.QueryableFactoryFactory>();
            services.TryAddScoped<IInterceptor, SqlServer.JsonSupportConnectionInterceptor>();

            return services;
        }
    }
}
#endif