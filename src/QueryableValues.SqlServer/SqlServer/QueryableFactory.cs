#if EFCORE
using BlazarTech.QueryableValues.Builders;
using BlazarTech.QueryableValues.Serializers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace BlazarTech.QueryableValues.SqlServer
{
    internal abstract class QueryableFactory : IQueryableFactory
    {
        protected const string SqlSelect = "SELECT";
        protected const string SqlSelectTop = "SELECT TOP({1})";

        protected static readonly ConcurrentDictionary<object, string> SqlCache = new();
        private static readonly ConcurrentDictionary<Type, object> SelectorExpressionCache = new();

        protected static readonly DefaultObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPool<StringBuilder>(
            new StringBuilderPooledObjectPolicy
            {
                InitialCapacity = 1024,
                MaximumRetainedCapacity = 16384
            });

        private readonly ISerializer _serializer;
        private readonly QueryableValuesSqlServerOptions _options;

        public QueryableFactory(ISerializer serializer, IDbContextOptions dbContextOptions)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (dbContextOptions is null)
            {
                throw new ArgumentNullException(nameof(dbContextOptions));
            }

            var extension = dbContextOptions.FindExtension<QueryableValuesSqlServerExtension>() ?? throw new InvalidOperationException($"{nameof(QueryableValuesSqlServerExtension)} not found.");

            _serializer = serializer;
            _options = extension.Options;
        }

        /// <summary>
        /// Used to optimize the generated SQL by providing a TOP(n) on the SELECT statement.
        /// In my tests, I observed improved memory grant estimation by SQL Server's query engine.
        /// </summary>
        protected bool UseSelectTopOptimization<T>(DeferredValues<T> deferredValues)
            where T : notnull
        {
#if EFCORE3
                // In my EF Core 3 tests, it seems that on the first execution of the query,
                // it is caching the values from the parameters provided to the FromSqlRaw method.
                // This imposes a problem when trying to optimize the SQL using the HasCount property in this class.
                // It is critical to know the exact number of elements behind "values" at execution time,
                // this is because the number of items behind "values" can change between executions of the query,
                // therefore, this optimization cannot be done in a reliable way under EF Core 3.
                //
                // Under EF Core 5 and 6 this is not an issue. The parameters are always evaluated on each execution.
                return false;
#else
            return
                _options.WithUseSelectTopOptimization &&
                deferredValues.HasCount;
#endif
        }

        protected abstract SqlParameter GetValuesParameter();

        private SqlParameter[] GetSqlParameters<T>(DeferredValues<T> deferredValues)
            where T : notnull
        {
            SqlParameter[] sqlParameters;

            var valuesParameter = GetValuesParameter();

            // Missing parameter names are auto-generated (p0, p1, etc.) by FromSqlRaw based on its position in the array.
            valuesParameter.ParameterName = null;

            // DeferredValues allows us to defer the enumeration of values until the query is materialized.
            valuesParameter.Value = _options.WithUseDeferredEnumeration ? deferredValues : deferredValues.ToString(null);

            if (UseSelectTopOptimization(deferredValues))
            {
                // bigint to avoid implicit casting by the TOP operation (observed in the execution plan).
                var countParameter = new SqlParameter(null, SqlDbType.BigInt)
                {
                    Value = _options.WithUseDeferredEnumeration ? deferredValues : deferredValues.ToInt64(null)
                };

                sqlParameters = new[] { valuesParameter, countParameter };
            }
            else
            {
                sqlParameters = new[] { valuesParameter };
            }

            return sqlParameters;
        }

        protected abstract string GetSqlForSimpleTypesByte(DeferredValues<byte> deferredValues);
        protected abstract string GetSqlForSimpleTypesInt16(DeferredValues<short> deferredValues);
        protected abstract string GetSqlForSimpleTypesInt32(DeferredValues<int> deferredValues);
        protected abstract string GetSqlForSimpleTypesInt64(DeferredValues<long> deferredValues);
        protected abstract string GetSqlForSimpleTypesDecimal(DeferredValues<decimal> deferredValues, (int Precision, int Scale) precisionScale);
        protected abstract string GetSqlForSimpleTypesSingle(DeferredValues<float> deferredValues);
        protected abstract string GetSqlForSimpleTypesDouble(DeferredValues<double> deferredValues);
        protected abstract string GetSqlForSimpleTypesDateTime(DeferredValues<DateTime> deferredValues);
        protected abstract string GetSqlForSimpleTypesDateTimeOffset(DeferredValues<DateTimeOffset> deferredValues);
        protected abstract string GetSqlForSimpleTypesChar(DeferredValues<char> deferredValues, bool isUnicode);
        protected abstract string GetSqlForSimpleTypesString(DeferredValues<string> deferredValues, bool isUnicode);
        protected abstract string GetSqlForSimpleTypesGuid(DeferredValues<Guid> deferredValues);

        private IQueryable<TValue> Create<TValue>(DbContext dbContext, string sql, DeferredValues<TValue> deferredValues)
            where TValue : notnull
        {
            var sqlParameters = GetSqlParameters(deferredValues);

            var queryableValues = dbContext
                .Set<QueryableValuesEntity<TValue>>()
                .FromSqlRaw(sql, sqlParameters);

            return queryableValues.Select(i => i.V);
        }

        public IQueryable<byte> Create(DbContext dbContext, IEnumerable<byte> values)
        {
            var deferredValues = new DeferredByteValues(_serializer, values);
            var sql = GetSqlForSimpleTypesByte(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<short> Create(DbContext dbContext, IEnumerable<short> values)
        {
            var deferredValues = new DeferredInt16Values(_serializer, values);
            var sql = GetSqlForSimpleTypesInt16(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<int> Create(DbContext dbContext, IEnumerable<int> values)
        {
            var deferredValues = new DeferredInt32Values(_serializer, values);
            var sql = GetSqlForSimpleTypesInt32(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<long> Create(DbContext dbContext, IEnumerable<long> values)
        {
            var deferredValues = new DeferredInt64Values(_serializer, values);
            var sql = GetSqlForSimpleTypesInt64(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<decimal> Create(DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals = 4)
        {
            var deferredValues = new DeferredDecimalValues(_serializer, values);
            var precisionScale = (38, numberOfDecimals);
            var sql = GetSqlForSimpleTypesDecimal(deferredValues, precisionScale);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<float> Create(DbContext dbContext, IEnumerable<float> values)
        {
            var deferredValues = new DeferredSingleValues(_serializer, values);
            var sql = GetSqlForSimpleTypesSingle(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<double> Create(DbContext dbContext, IEnumerable<double> values)
        {
            var deferredValues = new DeferredDoubleValues(_serializer, values);
            var sql = GetSqlForSimpleTypesDouble(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<DateTime> Create(DbContext dbContext, IEnumerable<DateTime> values)
        {
            var deferredValues = new DeferredDateTimeValues(_serializer, values);
            var sql = GetSqlForSimpleTypesDateTime(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<DateTimeOffset> Create(DbContext dbContext, IEnumerable<DateTimeOffset> values)
        {
            var deferredValues = new DeferredDateTimeOffsetValues(_serializer, values);
            var sql = GetSqlForSimpleTypesDateTimeOffset(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<char> Create(DbContext dbContext, IEnumerable<char> values, bool isUnicode = false)
        {
            var deferredValues = new DeferredCharValues(_serializer, values);
            var sql = GetSqlForSimpleTypesChar(deferredValues, isUnicode);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<string> Create(DbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            var deferredValues = new DeferredStringValues(_serializer, values);
            var sql = GetSqlForSimpleTypesString(deferredValues, isUnicode);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<Guid> Create(DbContext dbContext, IEnumerable<Guid> values)
        {
            var deferredValues = new DeferredGuidValues(_serializer, values);
            var sql = GetSqlForSimpleTypesGuid(deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        protected abstract string GetSqlForComplexTypes(
            IEntityOptionsBuilder entityOptions,
            bool useSelectTopOptimization,
            IReadOnlyList<EntityPropertyMapping> mappings
            );

        public IQueryable<TSource> Create<TSource>(DbContext dbContext, IEnumerable<TSource> values, Action<EntityOptionsBuilder<TSource>>? configure)
            where TSource : notnull
        {
            var simpleTypeQueryable = getSimpleTypeQueryable(dbContext, values);

            if (simpleTypeQueryable != null)
            {
                return simpleTypeQueryable;
            }

            var mappings = EntityPropertyMapping.GetMappings<TSource>();
            var deferredValues = new DeferredEntityValues<TSource>(_serializer, values, mappings);
            var useSelectTopOptimization = UseSelectTopOptimization(deferredValues);
            var sql = getSql(mappings, configure, useSelectTopOptimization);
            var sqlParameters = GetSqlParameters(deferredValues);

            var source = dbContext
                .Set<QueryableValuesEntity>()
                .FromSqlRaw(sql, sqlParameters);

            var projected = projectQueryable(source, mappings);

            return projected;

            string getSql(IReadOnlyList<EntityPropertyMapping> mappings, Action<EntityOptionsBuilder<TSource>>? configure, bool useSelectTopOptimization)
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
                    UseSelectTopOptimization = useSelectTopOptimization
                };

                if (SqlCache.TryGetValue(cacheKey, out string? sqlFromCache))
                {
                    return sqlFromCache;
                }

                var sql = GetSqlForComplexTypes(entityOptions, useSelectTopOptimization, mappings);

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

                static IQueryable<TSource>? getFromCache(Type sourceType, IQueryable<QueryableValuesEntity> source)
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

            IQueryable<TSource>? getSimpleTypeQueryable(DbContext dbContext, IEnumerable<TSource> values)
            {
                if (EntityPropertyMapping.IsSimpleType(typeof(TSource)))
                {
                    if (values is IEnumerable<byte> byteValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, byteValues);
                    }
                    else if (values is IEnumerable<short> int16Values)
                    {
                        return (IQueryable<TSource>)Create(dbContext, int16Values);
                    }
                    else if (values is IEnumerable<int> int32Values)
                    {
                        return (IQueryable<TSource>)Create(dbContext, int32Values);
                    }
                    else if (values is IEnumerable<long> int64Values)
                    {
                        return (IQueryable<TSource>)Create(dbContext, int64Values);
                    }
                    else if (values is IEnumerable<decimal> decimalValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, decimalValues);
                    }
                    else if (values is IEnumerable<float> singleValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, singleValues);
                    }
                    else if (values is IEnumerable<double> doubleValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, doubleValues);
                    }
                    else if (values is IEnumerable<DateTime> dateTimeValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, dateTimeValues);
                    }
                    else if (values is IEnumerable<DateTimeOffset> dateTimeOffsetValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, dateTimeOffsetValues);
                    }
                    else if (values is IEnumerable<Guid> guidValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, guidValues);
                    }
                    else if (values is IEnumerable<char> charValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, charValues);
                    }
                    else if (values is IEnumerable<string> stringValues)
                    {
                        return (IQueryable<TSource>)Create(dbContext, stringValues);
                    }
                    else
                    {
                        throw new NotImplementedException(typeof(TSource).FullName);
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
#endif