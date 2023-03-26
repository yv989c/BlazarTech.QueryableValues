#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazarTech.QueryableValues.SqlServer
{
    internal sealed class QueryableFactoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueryableValuesSqlServerOptions _options;

        public QueryableFactoryFactory(IServiceProvider serviceProvider, ExtensionOptions extensionOptions)
        {
            _serviceProvider = serviceProvider;
            _options = extensionOptions.Options;
        }

        public IQueryableFactory Create(DbContext dbContext)
        {
            var useJson = _options.WithSerializationOption switch
            {
                SqlServerSerialization.Auto => JsonSupportConnectionInterceptor.HasJsonSupport(dbContext).GetValueOrDefault(),
                SqlServerSerialization.UseJson => true,
                SqlServerSerialization.UseXml => false,
                _ => throw new NotImplementedException(),
            };

            if (useJson)
            {
                return _serviceProvider.GetRequiredService<JsonQueryableFactory>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<XmlQueryableFactory>();
            }
        }
    }
}
#endif
