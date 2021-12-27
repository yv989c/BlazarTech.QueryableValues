#if EFCORE
using BlazarTech.QueryableValues.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Extension methods provided by QueryableValues on the <see cref="DbContext"/> class.
    /// </summary>
    public static class QueryableValuesDbContextExtensions
    {
        private const string SqlSelect = "SELECT";
        private const string SqlSelectTop = "SELECT TOP({1})";

        private static readonly ConcurrentDictionary<object, string> SqlCache = new();
        private static readonly ConcurrentDictionary<Type, object> SelectorExpressionCache = new();

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

        private static SqlParameter[] GetSqlParameters<T>(DeferredValues<T> deferredValues)
            where T : notnull
        {
            SqlParameter[] sqlParameters;

            // Missing parameter names are auto-generated (p0, p1, etc.) by FromSqlRaw based on its position in the array.
            var xmlParameter = new SqlParameter(null, SqlDbType.Xml)
            {
                // DeferredValues allows us to defer the enumeration of values until the query is materialized.
                // Uses deferredValues.ToString() at evaluation time.
                Value = deferredValues
            };

            if (deferredValues.HasCount)
            {
                // bigint to avoid implicit casting by the TOP operation (observed in the execution plan).
                var countParameter = new SqlParameter(null, SqlDbType.BigInt)
                {
                    // Uses deferredValues.ToInt64() at evaluation time.
                    Value = deferredValues
                };

                sqlParameters = new[] { xmlParameter, countParameter };
            }
            else
            {
                sqlParameters = new[] { xmlParameter };
            }

            return sqlParameters;
        }

        private static IQueryable<TValue> GetQuery<TValue>(DbContext dbContext, string sql, DeferredValues<TValue> deferredValues)
            where TValue : notnull
        {
            EnsureConfigured(dbContext);

            var sqlParameters = GetSqlParameters(deferredValues);

            var queryableValues = dbContext
                .Set<QueryableValuesEntity<TValue>>()
                .FromSqlRaw(sql, sqlParameters);

            return queryableValues.Select(i => i.V);
        }

        private static string GetSqlForSimpleTypes<T>(string xmlType, string sqlType, DeferredValues<T> deferredValues, (int Precision, int Scale)? precisionScale = null)
            where T : notnull
        {
            var cacheKey = new
            {
                XmlType = xmlType,
                SqlType = sqlType,
                deferredValues.HasCount,
                PrecisionScale = precisionScale
            };

            if (SqlCache.TryGetValue(cacheKey, out string? sql))
            {
                return sql;
            }

            var sqlPrefix = deferredValues.HasCount ? SqlSelectTop : SqlSelect;
            var sqlTypeArguments = precisionScale.HasValue ? $"({precisionScale.Value.Precision},{precisionScale.Value.Scale})" : null;

            sql =
                $"{sqlPrefix} I.value('. cast as xs:{xmlType}?', '{sqlType}{sqlTypeArguments}') AS V " +
                "FROM {0}.nodes('/R/V') N(I)";

            SqlCache.TryAdd(cacheKey, sql);

            return sql;
        }

        private static void ValidateParameters<T>(DbContext dbContext, IEnumerable<T> values)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
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

            var deferredValues = new DeferredInt32Values(values);
            var sql = GetSqlForSimpleTypes("integer", "int", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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

            var deferredValues = new DeferredInt64Values(values);
            var sql = GetSqlForSimpleTypes("integer", "bigint", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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
        public static IQueryable<decimal> AsQueryableValues(this DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals)
        {
            ValidateParameters(dbContext, values);
            Validations.ValidateNumberOfDecimals(numberOfDecimals);

            var deferredValues = new DeferredDecimalValues(values);
            var precisionScale = (38, numberOfDecimals);
            var sql = GetSqlForSimpleTypes("decimal", "decimal", deferredValues, precisionScale: precisionScale);

            return GetQuery(dbContext, sql, deferredValues);
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

            var deferredValues = new DeferredDoubleValues(values);
            var sql = GetSqlForSimpleTypes("double", "float", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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

            var deferredValues = new DeferredDateTimeValues(values);
            var sql = GetSqlForSimpleTypes("dateTime", "datetime2", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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

            var deferredValues = new DeferredDateTimeOffsetValues(values);
            var sql = GetSqlForSimpleTypes("dateTime", "datetimeoffset", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<string> AsQueryableValues(this DbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            ValidateParameters(dbContext, values);

            string sql;
            var deferredValues = new DeferredStringValues(values);

            if (isUnicode)
            {
                sql = GetSqlForSimpleTypes("string", "nvarchar(max)", deferredValues);
            }
            else
            {
                sql = GetSqlForSimpleTypes("string", "varchar(max)", deferredValues);
            }

            return GetQuery(dbContext, sql, deferredValues);
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

            var deferredValues = new DeferredGuidValues(values);
            var sql = GetSqlForSimpleTypes("string", "uniqueidentifier", deferredValues);

            return GetQuery(dbContext, sql, deferredValues);
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
            EnsureConfigured(dbContext);

            if (EntityPropertyMapping.IsSimpleType(typeof(TSource)))
            {
                throw new ArgumentException("This method signature is intended for complex types only.", nameof(TSource));
            }

            var mappings = EntityPropertyMapping.GetMappings<TSource>();
            var deferredValues = new DeferredEntityValues<TSource>(values, mappings);
            var sql = getSql(mappings, configure, deferredValues.HasCount);
            var sqlParameters = GetSqlParameters(deferredValues);

            var source = dbContext
                .Set<QueryableValuesEntity>()
                .FromSqlRaw(sql, sqlParameters);

            var projected = projectQueryable(source, mappings);

            return projected;

            static string getSql(IReadOnlyList<EntityPropertyMapping> mappings, Action<EntityOptionsBuilder<TSource>>? configure, bool hasCount)
            {
                IEntityOptionsBuilder entityOptions;

                if (configure != null)
                {
                    var entityOptionsHelper = new EntityOptionsBuilder<TSource>();
                    configure?.Invoke(entityOptionsHelper);
                    entityOptions = entityOptionsHelper;
                }
                else
                {
                    entityOptions = new EntityOptionsBuilder<TSource>();
                }

                var cacheKey = new
                {
                    Options = entityOptions,
                    HasCount = hasCount
                };

                if (SqlCache.TryGetValue(cacheKey, out string? sqlFromCache))
                {
                    return sqlFromCache;
                }

                var sb = new StringBuilder(500);

                if (hasCount)
                {
                    sb.Append(SqlSelectTop);
                }
                else
                {
                    sb.Append(SqlSelect);
                }

                sb.AppendLine();

                for (int i = 0; i < mappings.Count; i++)
                {
                    var mapping = mappings[i];
                    var propertyOptions = entityOptions.GetPropertyOptions(mapping.Source);

                    if (i > 0)
                    {
                        sb.Append(',').AppendLine();
                    }

                    var targetName = mapping.Target.Name;

                    sb.Append("\tI.value('@").Append(targetName).Append("[1] cast as ");

                    switch (mapping.TypeName)
                    {
                        case EntityPropertyTypeName.Int:
                            sb.Append("xs:integer?', 'int'");
                            break;
                        case EntityPropertyTypeName.Long:
                            sb.Append("xs:integer?', 'bigint'");
                            break;
                        case EntityPropertyTypeName.Decimal:
                            {
                                var numberOfDecimals = propertyOptions?.NumberOfDecimals ?? entityOptions.DefaultForNumberOfDecimals;
                                sb.Append("xs:decimal?', 'decimal(38, ").Append(numberOfDecimals).Append(")'");
                            }
                            break;
                        case EntityPropertyTypeName.Double:
                            sb.Append("xs:double?', 'float'");
                            break;
                        case EntityPropertyTypeName.DateTime:
                            sb.Append("xs:dateTime?', 'datetime2'");
                            break;
                        case EntityPropertyTypeName.DateTimeOffset:
                            sb.Append("xs:dateTime?', 'datetimeoffset'");
                            break;
                        case EntityPropertyTypeName.Guid:
                            sb.Append("xs:string?', 'uniqueidentifier'");
                            break;
                        case EntityPropertyTypeName.String:
                            if ((propertyOptions?.IsUnicode ?? entityOptions.DefaultForIsUnicode) == true)
                            {
                                sb.Append("xs:string?', 'nvarchar(max)'");
                            }
                            else
                            {
                                sb.Append("xs:string?', 'varchar(max)'");
                            }
                            break;
                        default:
                            throw new NotImplementedException(mapping.TypeName.ToString());
                    }

                    sb.Append(") AS [").Append(targetName).Append(']');
                }

                sb.AppendLine();
                sb.Append("FROM {0}.nodes('/R/V') N(I)");

                var sql = sb.ToString();

                SqlCache.TryAdd(cacheKey, sql);

                return sql;
            }

            static IQueryable<TSource> projectQueryable(IQueryable<QueryableValuesEntity> source, IReadOnlyList<EntityPropertyMapping> mappings)
            {
                Type sourceType = typeof(TSource);

                var queryable = getFromCache(sourceType, source);
                if (queryable != null)
                {
                    return queryable;
                }

                Expression body;
                var parameterExpression = Expression.Parameter(typeof(QueryableValuesEntity), "i");

                var useConstructor = !mappings.All(i => i.Source.CanWrite);

                // Mainly for anonymous types.
                if (useConstructor)
                {
                    var constructor = sourceType.GetConstructors().FirstOrDefault();

                    if (constructor == null)
                    {
                        throw new InvalidOperationException($"Cannot find a suitable constructor in {sourceType.FullName}.");
                    }

                    var arguments = new Expression[mappings.Count];
                    var members = new MemberInfo[mappings.Count];

                    for (int i = 0; i < mappings.Count; i++)
                    {
                        var mapping = mappings[i];

                        arguments[i] = getTargetPropertyExpression(parameterExpression, mapping);

                        var methodInfo = mapping.Source.GetGetMethod(true);

                        if (methodInfo == null)
                        {
                            throw new InvalidOperationException($"Property {mapping.Source.Name} must have a Get accessor.");
                        }

                        members[i] = methodInfo;
                    }

                    body = Expression.New(constructor, arguments, members);
                }
                else
                {
                    var bindings = new MemberBinding[mappings.Count];

                    for (int i = 0; i < mappings.Count; i++)
                    {
                        var mapping = mappings[i];

                        var methodInfo = mapping.Source.GetSetMethod();

                        if (methodInfo == null)
                        {
                            throw new InvalidOperationException($"Property {mapping.Source.Name} must have a Set accessor.");
                        }

                        bindings[i] = Expression.Bind(
                            methodInfo,
                            getTargetPropertyExpression(parameterExpression, mapping)
                            );
                    }

                    var newExpression = Expression.New(sourceType);
                    body = Expression.MemberInit(newExpression, bindings);
                }

                var bodyParameteters = new[]
                {
                    parameterExpression
                };

                var selector = Expression.Lambda<Func<QueryableValuesEntity, TSource>>(body, bodyParameteters);

                SelectorExpressionCache.TryAdd(sourceType, selector);

                queryable = Queryable.Select(source, selector);

                return queryable;

                #region Helpers

                static Expression getTargetPropertyExpression(ParameterExpression parameterExpression, EntityPropertyMapping mapping)
                {
                    var propertyExpression = Expression.Property(parameterExpression, mapping.Target.Name);

                    if (mapping.Source.PropertyType == mapping.Target.PropertyType)
                    {
                        return propertyExpression;
                    }
                    else
                    {
                        return Expression.Convert(propertyExpression, mapping.Source.PropertyType);
                    }
                }

                static IQueryable<TSource>? getFromCache(Type sourceType, IQueryable< QueryableValuesEntity> source)
                {
                    if (SelectorExpressionCache.TryGetValue(sourceType, out object? selectorFromCache))
                    {
                        var selector = (Expression<Func<QueryableValuesEntity, TSource>>)selectorFromCache;
                        var queryable = Queryable.Select(source, selector);
                        return queryable;
                    }
                    else
                    {
                        return null;
                    }
                }

                #endregion
            }
        }
    }
}
#endif