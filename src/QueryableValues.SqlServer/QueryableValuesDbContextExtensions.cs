#if EFCORE
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Extension methods provided by QueryableValues on the <see cref="DbContext"/> class.
    /// </summary>
    public static class QueryableValuesDbContextExtensions
    {
        private static readonly Dictionary<int, string> QueryDecimalCache = new();

        private static void EnsureConfigured(DbContext dbContext)
        {
            var options = dbContext.GetService<IDbContextOptions>();
            var extension = options.FindExtension<DbContextOptionsExtension>();

            if (extension is null)
            {
                var message = $"{nameof(QueryableValues)} have not been configured for {dbContext.GetType().Name}. " +
                    "More info: https://github.com/yv989c/BlazarTech.QueryableValues#configuration";

                throw new InvalidOperationException(message);
            }
        }

        private static IQueryable<TValue> GetQuery<TValue>(DbContext dbContext, string sql, DeferredValues<TValue> deferredValues)
            where TValue : notnull
        {
            EnsureConfigured(dbContext);

            // Parameter name not provided so EF can auto generate one.
            var xmlParameter = new SqlParameter(null, SqlDbType.Xml)
            {
                // DeferredValues allows us to defer the enumeration of values until the query is materialized.
                Value = deferredValues
            };

            var queryableValues = dbContext
                .Set<QueryableValuesEntity<TValue>>()
                .FromSqlRaw(sql, xmlParameter);

            return queryableValues.Select(i => i.V);
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Int32}">IEnumerable&lt;int&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Int32}">IQueryable&lt;int&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<int> AsQueryableValues(this DbContext dbContext, IEnumerable<int> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:integer?', 'int') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredInt32Values(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Int64}">IEnumerable&lt;long&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Int64}">IQueryable&lt;long&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<long> AsQueryableValues(this DbContext dbContext, IEnumerable<long> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:integer?', 'bigint') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredInt64Values(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Decimal}">IEnumerable&lt;decimal&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="numberOfDecimals">Number of decimals (scale in SQL Server) to use when composing the <paramref name="values"/>.</param>
        /// <returns>An <see cref="IQueryable{Decimal}">IQueryable&lt;decimal&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<decimal> AsQueryableValues(this DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals)
        {
            if (numberOfDecimals < 0)
            {
                throw new ArgumentException("Cannot be negative.", nameof(numberOfDecimals));
            }

            if (numberOfDecimals > 38)
            {
                throw new ArgumentException("Cannot be greater than 38.", nameof(numberOfDecimals));
            }

            if (!QueryDecimalCache.TryGetValue(numberOfDecimals, out string? sql))
            {
                lock (QueryDecimalCache)
                {
                    if (!QueryDecimalCache.TryGetValue(numberOfDecimals, out sql))
                    {
                        sql =
                            $"SELECT I.value('. cast as xs:decimal?', 'decimal(38, {numberOfDecimals})') AS V " +
                            "FROM {0}.nodes('/R/V') N(I)";

                        QueryDecimalCache.Add(numberOfDecimals, sql);
                    }
                }
            }

            return GetQuery(dbContext, sql, new DeferredDecimalValues(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Double}">IEnumerable&lt;double&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Double}">IQueryable&lt;double&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<double> AsQueryableValues(this DbContext dbContext, IEnumerable<double> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:double?', 'float') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredDoubleValues(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{DateTime}">IEnumerable&lt;DateTime&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{DateTime}">IQueryable&lt;DateTime&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<DateTime> AsQueryableValues(this DbContext dbContext, IEnumerable<DateTime> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:dateTime?', 'datetime2') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredDateTimeValues(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{DateTimeOffset}">IEnumerable&lt;DateTimeOffset&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{DateTimeOffset}">IQueryable&lt;DateTimeOffset&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<DateTimeOffset> AsQueryableValues(this DbContext dbContext, IEnumerable<DateTimeOffset> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:dateTime?', 'datetimeoffset') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredDateTimeOffsetValues(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{String}">IEnumerable&lt;string&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <param name="isUnicode">If <c>true</c>, will cast the <paramref name="values"/> as <c>nvarchar</c>, otherwise, <c>varchar</c>.</param>
        /// <returns>An <see cref="IQueryable{String}">IQueryable&lt;string&gt;</see> that can be composed with other entities in the query.</returns>
        /// <remarks>
        /// About Performance: If the result is going to be composed against the property of an entity that uses 
        /// unicode (<c>nvarchar</c>), then <paramref name="isUnicode"/> should be <c>true</c>.
        /// Failing to do this may force SQL Server's query engine to do an implicit casting, which results 
        /// in a scan instead of an index seek (assuming there's a covering index).
        /// </remarks>
        public static IQueryable<string> AsQueryableValues(this DbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            string sql;

            if (isUnicode)
            {
                sql =
                    "SELECT I.value('. cast as xs:string?', 'nvarchar(max)') AS V " +
                    "FROM {0}.nodes('/R/V') N(I)";
            }
            else
            {
                sql =
                    "SELECT I.value('. cast as xs:string?', 'varchar(max)') AS V " +
                    "FROM {0}.nodes('/R/V') N(I)";
            }

            return GetQuery(dbContext, sql, new DeferredStringValues(values));
        }

        /// <summary>
        /// Allows an <see cref="IEnumerable{Guid}">IEnumerable&lt;Guid&gt;</see> to be composed in an Entity Framework query.
        /// </summary>
        /// <param name="dbContext">The <see cref="DbContext"/> owning the query.</param>
        /// <param name="values">The sequence of values to compose.</param>
        /// <returns>An <see cref="IQueryable{Guid}">IQueryable&lt;Guid&gt;</see> that can be composed with other entities in the query.</returns>
        public static IQueryable<Guid> AsQueryableValues(this DbContext dbContext, IEnumerable<Guid> values)
        {
            const string sql =
                "SELECT I.value('. cast as xs:string?', 'uniqueidentifier') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            return GetQuery(dbContext, sql, new DeferredGuidValues(values));
        }


        private class PropertyMapping
        {
            public PropertyInfo? Source { get; set; }
            public PropertyInfo? Target { get; set; }
        }

        private static IEnumerable<PropertyMapping> GetPropertyMappings<T>()
        {
            var sourceProperties = typeof(T).GetProperties();

            var targetPropertiesByType = (
                from i in typeof(QueryableValuesEntity).GetProperties()
                group i by normalizeType(i.PropertyType) into g
                select g
                )
                .ToDictionary(k => k.Key, v => new Queue<PropertyInfo>(v));

            foreach (var sourceProperty in sourceProperties)
            {
                var propertyType = normalizeType(sourceProperty.PropertyType);

                if (targetPropertiesByType.TryGetValue(propertyType, out Queue<PropertyInfo>? targetProperties))
                {
                    yield return new PropertyMapping
                    {
                        Source = sourceProperty,
                        Target = targetProperties.Dequeue()
                    };
                }
            }

            static Type normalizeType(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        }

        // todos:
        // - Add DateOnly for Core 6 (think about TimeOnly).
        // - Add Test case for Database Script/Migrations apis. Ensure that the internal entity is not leaked.
        // - Fix documentation (remove ef-core5 reference in the url)

        public static IQueryable<TestEntity> AsQueryableValuesTest(this DbContext dbContext, IEnumerable<TestEntity> values)
        {
            var testType = new { Asd = 1, asd2 = "Hi", asd3 = 123 };

            static void ha<T>(T o)
            {
                var mappings1 = GetPropertyMappings<T>().ToList();
            }

            ha(testType);

            var mappings2 = GetPropertyMappings<TestEntity>().ToList();


            // todo: Use properties instead of elements? how do I express null? lack of the property? may be.... this way, only non-null properties are going to be rendered in the XML.
            var xml = "<R><V><Int1>1</Int1></V></R>";

            const string sql =
                "SELECT I.value('. cast as xs:integer?', 'int') AS Int1 " +
                "FROM {0}.nodes('/R/V/Int1') N(I)";

            EnsureConfigured(dbContext);

            // Parameter name not provided so EF can auto generate one.
            var xmlParameter = new SqlParameter(null, SqlDbType.Xml)
            {
                // DeferredValues allows us to defer the enumeration of values until the query is materialized.
                //Value = new DeferredValues<TValue>(values)
                Value = xml
            };

            var queryableValues = dbContext
                .Set<QueryableValuesEntity>()
                .FromSqlRaw(sql, xmlParameter);

            var result = queryableValues
                .Select(i => new { i.Int1 })
                // todo: always convert to the target's type.
                .Select(i => new TestEntity { Id = (int)i.Int1 });

            return result;
        }
    }
}
#endif