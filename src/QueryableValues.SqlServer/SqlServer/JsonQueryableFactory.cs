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

                sb.Append(" [").Append(QueryableValuesEntity.IndexPropertyName).Append(']');

                for (var i = 0; i < mappings.Count; i++)
                {
                    sb.Append(", [").Append(mappings[i].Target.Name).Append(']');
                }

                sb.AppendLine();
                sb.Append("FROM OPENJSON({0}) WITH ([").Append(QueryableValuesEntity.IndexPropertyName).Append("] int");

                for (var i = 0; i < mappings.Count; i++)
                {
                    var mapping = mappings[i];
                    var propertyOptions = entityOptions.GetPropertyOptions(mapping.Source);

                    sb.Append(", ");

                    var targetName = mapping.Target.Name;

                    sb.Append('[').Append(targetName).Append("] ");

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

                sb.Append(')').AppendLine();
                sb.Append("ORDER BY [").Append(QueryableValuesEntity.IndexPropertyName).Append(']');

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