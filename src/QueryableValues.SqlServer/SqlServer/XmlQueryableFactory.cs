#if EFCORE
using BlazarTech.QueryableValues.Builders;
using BlazarTech.QueryableValues.Serializers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    internal sealed class XmlQueryableFactory : IQueryableFactory
    {
        private const string SqlSelect = "SELECT";
        private const string SqlSelectTop = "SELECT TOP({1})";

        private static readonly ConcurrentDictionary<object, string> SqlCache = new();
        private static readonly ConcurrentDictionary<Type, object> SelectorExpressionCache = new();

        private readonly IXmlSerializer _xmlSerializer;

        public XmlQueryableFactory(IXmlSerializer xmlSerializer)
        {
            _xmlSerializer = xmlSerializer;
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
                //Value = deferredValues.SqlXmlValue()
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

        private static IQueryable<TValue> Create<TValue>(DbContext dbContext, string sql, DeferredValues<TValue> deferredValues)
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
            var deferredValues = new DeferredByteValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("unsignedByte", "tinyint", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<short> Create(DbContext dbContext, IEnumerable<short> values)
        {
            var deferredValues = new DeferredInt16Values(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("short", "smallint", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<int> Create(DbContext dbContext, IEnumerable<int> values)
        {
            var deferredValues = new DeferredInt32Values(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("integer", "int", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<long> Create(DbContext dbContext, IEnumerable<long> values)
        {
            var deferredValues = new DeferredInt64Values(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("integer", "bigint", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<decimal> Create(DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals = 4)
        {
            var deferredValues = new DeferredDecimalValues(_xmlSerializer, values);
            var precisionScale = (38, numberOfDecimals);
            var sql = GetSqlForSimpleTypes("decimal", "decimal", deferredValues, precisionScale: precisionScale);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<float> Create(DbContext dbContext, IEnumerable<float> values)
        {
            var deferredValues = new DeferredSingleValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("float", "real", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<double> Create(DbContext dbContext, IEnumerable<double> values)
        {
            var deferredValues = new DeferredDoubleValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("double", "float", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<DateTime> Create(DbContext dbContext, IEnumerable<DateTime> values)
        {
            var deferredValues = new DeferredDateTimeValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("dateTime", "datetime2", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<DateTimeOffset> Create(DbContext dbContext, IEnumerable<DateTimeOffset> values)
        {
            var deferredValues = new DeferredDateTimeOffsetValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("dateTime", "datetimeoffset", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<char> Create(DbContext dbContext, IEnumerable<char> values, bool isUnicode = false)
        {
            string sql;
            var deferredValues = new DeferredCharValues(_xmlSerializer, values);

            if (isUnicode)
            {
                sql = GetSqlForSimpleTypes("string", "nvarchar(1)", deferredValues);
            }
            else
            {
                sql = GetSqlForSimpleTypes("string", "varchar(1)", deferredValues);
            }

            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<string> Create(DbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            string sql;
            var deferredValues = new DeferredStringValues(_xmlSerializer, values);

            if (isUnicode)
            {
                sql = GetSqlForSimpleTypes("string", "nvarchar(max)", deferredValues);
            }
            else
            {
                sql = GetSqlForSimpleTypes("string", "varchar(max)", deferredValues);
            }

            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<Guid> Create(DbContext dbContext, IEnumerable<Guid> values)
        {
            var deferredValues = new DeferredGuidValues(_xmlSerializer, values);
            var sql = GetSqlForSimpleTypes("string", "uniqueidentifier", deferredValues);
            return Create(dbContext, sql, deferredValues);
        }

        public IQueryable<TSource> Create<TSource>(DbContext dbContext, IEnumerable<TSource> values, Action<EntityOptionsBuilder<TSource>>? configure) where TSource : notnull
        {
            var simpleTypeQueryable = getSimpleTypeQueryable(dbContext, values);

            if (simpleTypeQueryable != null)
            {
                return simpleTypeQueryable;
            }

            var mappings = EntityPropertyMapping.GetMappings<TSource>();
            var deferredValues = new DeferredEntityValues<TSource>(_xmlSerializer, values, mappings);
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
                        case EntityPropertyTypeName.Boolean:
                            sb.Append("xs:boolean?', 'bit'");
                            break;
                        case EntityPropertyTypeName.Byte:
                            sb.Append("xs:unsignedByte?', 'tinyint'");
                            break;
                        case EntityPropertyTypeName.Int16:
                            sb.Append("xs:short?', 'smallint'");
                            break;
                        case EntityPropertyTypeName.Int32:
                            sb.Append("xs:integer?', 'int'");
                            break;
                        case EntityPropertyTypeName.Int64:
                            sb.Append("xs:integer?', 'bigint'");
                            break;
                        case EntityPropertyTypeName.Decimal:
                            {
                                var numberOfDecimals = propertyOptions?.NumberOfDecimals ?? entityOptions.DefaultForNumberOfDecimals;
                                sb.Append("xs:decimal?', 'decimal(38, ").Append(numberOfDecimals).Append(")'");
                            }
                            break;
                        case EntityPropertyTypeName.Single:
                            sb.Append("xs:float?', 'real'");
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
                        case EntityPropertyTypeName.Char:
                            if ((propertyOptions?.IsUnicode ?? entityOptions.DefaultForIsUnicode) == true)
                            {
                                sb.Append("xs:string?', 'nvarchar(1)'");
                            }
                            else
                            {
                                sb.Append("xs:string?', 'varchar(1)'");
                            }
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