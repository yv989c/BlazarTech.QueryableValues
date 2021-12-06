#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Extension methods for the configuration of QueryableValues.
    /// </summary>
    public static class QueryableValuesDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures QueryableValues so the <c>AsQueryableValues</c> extension method can be used on a <see cref="DbContext"/> class.
        /// </summary>
        /// <returns>The <paramref name="optionsBuilder"/> so subsequent configurations can be chained.</returns>
        public static SqlServerDbContextOptionsBuilder UseQueryableValues(this SqlServerDbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder is null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;
            var extension = coreOptionsBuilder.Options.FindExtension<DbContextOptionsExtension>() ?? new DbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }
    }
}
#endif