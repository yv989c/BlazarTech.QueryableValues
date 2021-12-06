#if TESTS
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    public class QueryableValuesDbContextOptionsExtensionTests
    {
        [Fact]
        public void MustReplaceService()
        {
            var services = new ServiceCollection();

            services.AddTransient<IModelCustomizer, FakeModelCustomizer>();

            var extension = new DbContextOptionsExtension();

            extension.ApplyServices(services);

            var serviceProvider = services.BuildServiceProvider();

            var ourModelCustomizer = serviceProvider.GetService<IModelCustomizer>();
            Assert.IsType<ModelCustomizer<FakeModelCustomizer>>(ourModelCustomizer);

            var theirsModelCustomizer = serviceProvider.GetService<FakeModelCustomizer>();
            Assert.IsType<FakeModelCustomizer>(theirsModelCustomizer);
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