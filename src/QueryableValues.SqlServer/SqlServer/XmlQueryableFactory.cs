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

                sb
                    .Append("\tI.value('@")
                    .Append(QueryableValuesEntity.IndexPropertyName)
                    .Append(" cast as xs:integer?', 'int') AS [")
                    .Append(QueryableValuesEntity.IndexPropertyName)
                    .Append(']');

                foreach (var mapping in mappings)
                {
                    var propertyOptions = entityOptions.GetPropertyOptions(mapping.Source);

                    sb.Append(',').AppendLine();

                    var targetName = mapping.Target.Name;

                    sb.Append("\tI.value('@").Append(targetName).Append(" cast as ");

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
                sb.Append("FROM {0}.nodes('/R[1]/V') N(I)").AppendLine();
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