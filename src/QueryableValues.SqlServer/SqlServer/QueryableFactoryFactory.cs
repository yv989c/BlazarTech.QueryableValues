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
            var useJson = _options.WithSerializationOptions switch
            {
                SerializationOptions.Auto => JsonSupportConnectionInterceptor.HasJsonSupport(dbContext).GetValueOrDefault(),
                SerializationOptions.UseJson => true,
                SerializationOptions.UseXml => false,
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
