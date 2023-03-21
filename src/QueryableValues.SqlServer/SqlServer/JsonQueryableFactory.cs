#if EFCORE
using BlazarTech.QueryableValues.Builders;
using BlazarTech.QueryableValues.Serializers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;

namespace BlazarTech.QueryableValues.SqlServer
{
    internal sealed class JsonQueryableFactory : QueryableFactory
    {
        public JsonQueryableFactory(IJsonSerializer serializer, IDbContextOptions dbContextOptions)
            : base(serializer, dbContextOptions)
        {
        }

        protected override SqlParameter GetValuesParameter()
        {
            return new SqlParameter(null, SqlDbType.NVarChar, -1);
        }

        private string GetSqlForSimpleTypes<T>(string sqlType, DeferredValues<T> deferredValues, (int Precision, int Scale)? precisionScale = null)
            where T : notnull
        {
            var useSelectTopOptimization = UseSelectTopOptimization(deferredValues);
            var cacheKey = new
            {
                SqlType = sqlType,
                UseSelectTopOptimization = useSelectTopOptimization,
                PrecisionScale = precisionScale
            };

            if (SqlCache.TryGetValue(cacheKey, out string? sql))
            {
                return sql;
            }

            var sqlPrefix = useSelectTopOptimization ? SqlSelectTop : SqlSelect;
            var sqlTypeArguments = precisionScale.HasValue ? $"({precisionScale.Value.Precision},{precisionScale.Value.Scale})" : null;

            sql =
                $"{sqlPrefix} V " +
                $"FROM OPENJSON({{0}}) WITH ([V] {sqlType}{sqlTypeArguments} '$')";

            SqlCache.TryAdd(cacheKey, sql);

            return sql;
        }

        protected override string GetSqlForSimpleTypesByte(DeferredValues<byte> deferredValues)
        {
            return GetSqlForSimpleTypes("tinyint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt16(DeferredValues<short> deferredValues)
        {
            return GetSqlForSimpleTypes("smallint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt32(DeferredValues<int> deferredValues)
        {
            return GetSqlForSimpleTypes("int", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt64(DeferredValues<long> deferredValues)
        {
            return GetSqlForSimpleTypes("bigint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDecimal(DeferredValues<decimal> deferredValues, (int Precision, int Scale) precisionScale)
        {
            return GetSqlForSimpleTypes("decimal", deferredValues, precisionScale: precisionScale);
        }

        protected override string GetSqlForSimpleTypesSingle(DeferredValues<float> deferredValues)
        {
            return GetSqlForSimpleTypes("real", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDouble(DeferredValues<double> deferredValues)
        {
            return GetSqlForSimpleTypes("float", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDateTime(DeferredValues<DateTime> deferredValues)
        {
            return GetSqlForSimpleTypes("datetime2", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDateTimeOffset(DeferredValues<DateTimeOffset> deferredValues)
        {
            return GetSqlForSimpleTypes("datetimeoffset", deferredValues);
        }

        protected override string GetSqlForSimpleTypesChar(DeferredValues<char> deferredValues, bool isUnicode)
        {
            return GetSqlForSimpleTypes(isUnicode ? "nvarchar(1)" : "varchar(1)", deferredValues);
        }

        protected override string GetSqlForSimpleTypesString(DeferredValues<string> deferredValues, bool isUnicode)
        {
            return GetSqlForSimpleTypes(isUnicode ? "nvarchar(max)" : "varchar(max)", deferredValues);
        }

        protected override string GetSqlForSimpleTypesGuid(DeferredValues<Guid> deferredValues)
        {
            return GetSqlForSimpleTypes("uniqueidentifier", deferredValues);
        }

        protected override string GetSqlForComplexTypes(IEntityOptionsBuilder entityOptions, bool useSelectTopOptimization, IReadOnlyList<EntityPropertyMapping> mappings)
        {
            var sb = StringBuilderPool.Get();

            try
            {
                if (useSelectTopOptimization)
                {
                    sb.Append(SqlSelectTop);
                }
                else
                {
                    sb.Append(SqlSelect);
                }

                sb.Append(' ');

                for (var i = 0; i < mappings.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append('[').Append(mappings[i].Target.Name).Append(']');
                }

                sb.AppendLine();
                sb.Append("FROM OPENJSON({0}) WITH (");
                sb.AppendLine();

                for (var i = 0; i < mappings.Count; i++)
                {
                    var mapping = mappings[i];
                    var propertyOptions = entityOptions.GetPropertyOptions(mapping.Source);

                    if (i > 0)
                    {
                        sb.Append(',').AppendLine();
                    }

                    var targetName = mapping.Target.Name;

                    sb.Append("\t[").Append(targetName).Append("] ");

                    switch (mapping.TypeName)
                    {
                        case EntityPropertyTypeName.Boolean:
                            sb.Append("bit");
                            break;
                        case EntityPropertyTypeName.Byte:
                            sb.Append("tinyint");
                            break;
                        case EntityPropertyTypeName.Int16:
                            sb.Append("smallint");
                            break;
                        case EntityPropertyTypeName.Int32:
                            sb.Append("int");
                            break;
                        case EntityPropertyTypeName.Int64:
                            sb.Append("bigint");
                            break;
                        case EntityPropertyTypeName.Decimal:
                            {
                                var numberOfDecimals = propertyOptions?.NumberOfDecimals ?? entityOptions.DefaultForNumberOfDecimals;
                                sb.Append("decimal(38, ").Append(numberOfDecimals).Append(')');
                            }
                            break;
                        case EntityPropertyTypeName.Single:
                            sb.Append("real");
                            break;
                        case EntityPropertyTypeName.Double:
                            sb.Append("float");
                            break;
                        case EntityPropertyTypeName.DateTime:
                            sb.Append("datetime2");
                            break;
                        case EntityPropertyTypeName.DateTimeOffset:
                            sb.Append("datetimeoffset");
                            break;
                        case EntityPropertyTypeName.Guid:
                            sb.Append("uniqueidentifier");
                            break;
                        case EntityPropertyTypeName.Char:
                            if ((propertyOptions?.IsUnicode ?? entityOptions.DefaultForIsUnicode) == true)
                            {
                                sb.Append("nvarchar(1)");
                            }
                            else
                            {
                                sb.Append("varchar(1)");
                            }
                            break;
                        case EntityPropertyTypeName.String:
                            if ((propertyOptions?.IsUnicode ?? entityOptions.DefaultForIsUnicode) == true)
                            {
                                sb.Append("nvarchar(max)");
                            }
                            else
                            {
                                sb.Append("varchar(max)");
                            }
                            break;
                        default:
                            throw new NotImplementedException(mapping.TypeName.ToString());
                    }
                }

                sb.AppendLine();
                sb.Append(')');

                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Return(sb);
            }
        }
    }
}
#endif