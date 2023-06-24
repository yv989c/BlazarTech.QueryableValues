#if TESTS
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    public class DependencyInjectionTests
    {
        private void MustReplaceServiceAssertion(IServiceProvider serviceProvider)
        {
            var ourModelCustomizer = serviceProvider.GetService<IModelCustomizer>();
            Assert.IsType<ModelCustomizer<FakeModelCustomizer>>(ourModelCustomizer);

            var theirsModelCustomizer = serviceProvider.GetService<FakeModelCustomizer>();
            Assert.IsType<FakeModelCustomizer>(theirsModelCustomizer);
        }

        [Fact]
        public void MustReplaceServiceViaApplyServices()
        {
            var services = new ServiceCollection();

            services.AddTransient<IModelCustomizer, FakeModelCustomizer>();

            var extension = new QueryableValuesSqlServerExtension();

            extension.ApplyServices(services);

            var serviceProvider = services.BuildServiceProvider();

            MustReplaceServiceAssertion(serviceProvider);
        }

        [Fact]
        public void MustReplaceServiceViaAddQueryableValuesSqlServer()
        {
            var services = new ServiceCollection();

            services.AddTransient<IModelCustomizer, FakeModelCustomizer>();

            services.AddQueryableValuesSqlServer();

            var serviceProvider = services.BuildServiceProvider();

            MustReplaceServiceAssertion(serviceProvider);
        }

        private class FakeModelCustomizer : IModelCustomizer
        {
            public void Customize(ModelBuilder modelBuilder, DbContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
#endif