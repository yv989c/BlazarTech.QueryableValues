#if EFCORE
using BlazarTech.QueryableValues.Builders;
using BlazarTech.QueryableValues.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Extension methods provided by QueryableValues on the <see cref="DbContext"/> class.
    /// </summary>
    public static class QueryableValuesDbContextExtensions
    {
        private static void ValidateParameters<T>(DbContext dbContext, IEnumerable<T> values)
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }
        }

        private static IQueryableFactory GetQueryableFactory(DbContext dbContext)
        {
            try
            {
                return dbContext.GetService<QueryableFactoryFactory>()?.Create() ?? throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                var message = $"{nameof(QueryableValues)} have not been configured for {dbContext.GetType().Name}. " +
                    "More info: https://github.com/yv989c/BlazarTech.QueryableValues#configuration";

                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Byte}">IEnumerable&lt;byte&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Byte}">IQueryable&lt;byte&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<byte> AsQueryableValues(this DbContext dbContext, IEnumerable<byte> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Int16}">IEnumerable&lt;short&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Int16}">IQueryable&lt;short&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<short> AsQueryableValues(this DbContext dbContext, IEnumerable<short> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Int32}">IEnumerable&lt;int&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Int32}">IQueryable&lt;int&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<int> AsQueryableValues(this DbContext dbContext, IEnumerable<int> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Int64}">IEnumerable&lt;long&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Int64}">IQueryable&lt;long&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<long> AsQueryableValues(this DbContext dbContext, IEnumerable<long> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Decimal}">IEnumerable&lt;decimal&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="numberOfDecimals">Number of decimals (scale in SQL Server) to use when composing the <paramref name="values"/>.</param>
        /// <returns>An <see cref="IQueryable{Decimal}">IQueryable&lt;decimal&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<decimal> AsQueryableValues(this DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals = 4)
        {
            ValidateParameters(dbContext, values);
            Validations.ValidateNumberOfDecimals(numberOfDecimals);
            return GetQueryableFactory(dbContext).Create(dbContext, values, numberOfDecimals);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Single}">IEnumerable&lt;float&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Single}">IQueryable&lt;float&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<float> AsQueryableValues(this DbContext dbContext, IEnumerable<float> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Double}">IEnumerable&lt;double&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Double}">IQueryable&lt;double&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<double> AsQueryableValues(this DbContext dbContext, IEnumerable<double> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{DateTime}">IEnumerable&lt;DateTime&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{DateTime}">IQueryable&lt;DateTime&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<DateTime> AsQueryableValues(this DbContext dbContext, IEnumerable<DateTime> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{DateTimeOffset}">IEnumerable&lt;DateTimeOffset&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{DateTimeOffset}">IQueryable&lt;DateTimeOffset&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<DateTimeOffset> AsQueryableValues(this DbContext dbContext, IEnumerable<DateTimeOffset> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Char}">IEnumerable&lt;char&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="isUnicode">If <see langword="true"/>, will cast the <paramref name="values"/> as <c>nvarchar</c>, otherwise, <c>varchar</c>.</param>
        /// <returns>An <see cref="IQueryable{Char}">IQueryable&lt;char&gt;</see> that can be composed with other entities in the query.</returns>
        /// <remarks>
        /// About Performance: If the result is going to be composed against the property of an entity that uses 
        /// unicode (<c>nvarchar</c>), then <paramref name="isUnicode"/> should be <see langword="true"/>.
        /// Failing to do this may force SQL Server's query engine to do an implicit casting, which results 
        /// in a scan instead of an index seek (assuming there's a covering index).
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<char> AsQueryableValues(this DbContext dbContext, IEnumerable<char> values, bool isUnicode = false)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values, isUnicode);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{String}">IEnumerable&lt;string&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="isUnicode">If <see langword="true"/>, will cast the <paramref name="values"/> as <c>nvarchar</c>, otherwise, <c>varchar</c>.</param>
        /// <returns>An <see cref="IQueryable{String}">IQueryable&lt;string&gt;</see> that can be composed with other entities in the query.</returns>
        /// <remarks>
        /// About Performance: If the result is going to be composed against the property of an entity that uses 
        /// unicode (<c>nvarchar</c>), then <paramref name="isUnicode"/> should be <see langword="true"/>.
        /// Failing to do this may force SQL Server's query engine to do an implicit casting, which results 
        /// in a scan instead of an index seek (assuming there's a covering index).
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<string> AsQueryableValues(this DbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values, isUnicode);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Guid}">IEnumerable&lt;Guid&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Guid}">IQueryable&lt;Guid&gt;</see> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<Guid> AsQueryableValues(this DbContext dbContext, IEnumerable<Guid> values)
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{T}"/> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="configure">Performs configuration.</param>
        /// <returns>An <see cref="IQueryable{T}"/> that can be composed with other entities in the query.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<TSource> AsQueryableValues<TSource>(this DbContext dbContext, IEnumerable<TSource> values, Action<EntityOptionsBuilder<TSource>>? configure = null)
            where TSource : notnull
        {
            ValidateParameters(dbContext, values);
            return GetQueryableFactory(dbContext).Create(dbContext, values, configure);
        }
    }
}
#endif