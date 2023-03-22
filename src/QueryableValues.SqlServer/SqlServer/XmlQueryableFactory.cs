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
    internal sealed class XmlQueryableFactory : QueryableFactory
    {
        public XmlQueryableFactory(IXmlSerializer serializer, IDbContextOptions dbContextOptions)
            : base(serializer, dbContextOptions)
        {
        }

        protected override SqlParameter GetValuesParameter()
        {
            return new SqlParameter(null, SqlDbType.Xml);
        }

        private string GetSqlForSimpleTypes<T>(string xmlType, string sqlType, DeferredValues<T> deferredValues, (int Precision, int Scale)? precisionScale = null)
            where T : notnull
        {
            var useSelectTopOptimization = UseSelectTopOptimization(deferredValues);
            var cacheKeyProperties = new
            {
                XmlType = xmlType,
                SqlType = sqlType,
                UseSelectTopOptimization = useSelectTopOptimization,
                PrecisionScale = precisionScale
            };

            var cacheKey = GetCacheKey(cacheKeyProperties);

            if (SqlCache.TryGetValue(cacheKey, out string? sql))
            {
                return sql;
            }

            var sqlPrefix = useSelectTopOptimization ? SqlSelectTop : SqlSelect;
            var sqlTypeArguments = precisionScale.HasValue ? $"({precisionScale.Value.Precision},{precisionScale.Value.Scale})" : null;

            sql =
                $"{sqlPrefix} I.value('. cast as xs:{xmlType}?', '{sqlType}{sqlTypeArguments}') [V] " +
                "FROM {0}.nodes('/R/V') N(I)";

            SqlCache.TryAdd(cacheKey, sql);

            return sql;
        }

        protected override string GetSqlForSimpleTypesByte(DeferredValues<byte> deferredValues)
        {
            return GetSqlForSimpleTypes("unsignedByte", "tinyint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt16(DeferredValues<short> deferredValues)
        {
            return GetSqlForSimpleTypes("short", "smallint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt32(DeferredValues<int> deferredValues)
        {
            return GetSqlForSimpleTypes("integer", "int", deferredValues);
        }

        protected override string GetSqlForSimpleTypesInt64(DeferredValues<long> deferredValues)
        {
            return GetSqlForSimpleTypes("integer", "bigint", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDecimal(DeferredValues<decimal> deferredValues, (int Precision, int Scale) precisionScale)
        {
            return GetSqlForSimpleTypes("decimal", "decimal", deferredValues, precisionScale: precisionScale);
        }

        protected override string GetSqlForSimpleTypesSingle(DeferredValues<float> deferredValues)
        {
            return GetSqlForSimpleTypes("float", "real", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDouble(DeferredValues<double> deferredValues)
        {
            return GetSqlForSimpleTypes("double", "float", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDateTime(DeferredValues<DateTime> deferredValues)
        {
            return GetSqlForSimpleTypes("dateTime", "datetime2", deferredValues);
        }

        protected override string GetSqlForSimpleTypesDateTimeOffset(DeferredValues<DateTimeOffset> deferredValues)
        {
            return GetSqlForSimpleTypes("dateTime", "datetimeoffset", deferredValues);
        }

        protected override string GetSqlForSimpleTypesChar(DeferredValues<char> deferredValues, bool isUnicode)
        {
            return GetSqlForSimpleTypes("string", isUnicode ? "nvarchar(1)" : "varchar(1)", deferredValues);
        }

        protected override string GetSqlForSimpleTypesString(DeferredValues<string> deferredValues, bool isUnicode)
        {
            return GetSqlForSimpleTypes("string", isUnicode ? "nvarchar(max)" : "varchar(max)", deferredValues);
        }

        protected override string GetSqlForSimpleTypesGuid(DeferredValues<Guid> deferredValues)
        {
            return GetSqlForSimpleTypes("string", "uniqueidentifier", deferredValues);
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