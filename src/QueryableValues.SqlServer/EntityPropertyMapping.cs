using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace BlazarTech.QueryableValues
{
    internal sealed class EntityPropertyMapping
    {
        private static readonly PropertyInfo[] EntityProperties = typeof(QueryableValuesEntity).GetProperties();
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<EntityPropertyMapping>> MappingCache = new ConcurrentDictionary<Type, IReadOnlyList<EntityPropertyMapping>>();

        private static readonly Type IntType = typeof(int);
        private static readonly Type LongType = typeof(long);
        private static readonly Type DecimalType = typeof(decimal);
        private static readonly Type DoubleType = typeof(double);
        private static readonly Type DateTimeType = typeof(DateTime);
        private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        private static readonly Type GuidType = typeof(Guid);
        private static readonly Type StringType = typeof(string);

        public PropertyInfo Source { get; }
        public PropertyInfo Target { get; }
        public Type NormalizedType { get; }
        public EntityPropertyTypeName TypeName { get; }

        private EntityPropertyMapping(PropertyInfo source, PropertyInfo target, Type normalizedType)
        {
            Source = source;
            Target = target;
            NormalizedType = normalizedType;

            if (normalizedType == IntType)
            {
                TypeName = EntityPropertyTypeName.Int;
            }
            else if (normalizedType == LongType)
            {
                TypeName = EntityPropertyTypeName.Long;
            }
            else if (normalizedType == DecimalType)
            {
                TypeName = EntityPropertyTypeName.Decimal;
            }
            else if (normalizedType == DoubleType)
            {
                TypeName = EntityPropertyTypeName.Double;
            }
            else if (normalizedType == DateTimeType)
            {
                TypeName = EntityPropertyTypeName.DateTime;
            }
            else if (normalizedType == DateTimeOffsetType)
            {
                TypeName = EntityPropertyTypeName.DateTimeOffset;
            }
            else if (normalizedType == GuidType)
            {
                TypeName = EntityPropertyTypeName.Guid;
            }
            else if (normalizedType == StringType)
            {
                TypeName = EntityPropertyTypeName.String;
            }
            else
            {
                throw new NotSupportedException($"{source.PropertyType.FullName} is not supported.");
            }
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings(Type sourceType)
        {
            if (MappingCache.TryGetValue(sourceType, out IReadOnlyList<EntityPropertyMapping> mappingsFromCache))
            {
                return mappingsFromCache;
            }

            var sourceProperties = sourceType.GetProperties();
            var mappings = new List<EntityPropertyMapping>(sourceProperties.Length);

            var targetPropertiesByType = (
                from i in EntityProperties
                group i by normalizeType(i.PropertyType) into g
                select g
                )
                .ToDictionary(k => k.Key, v => new Queue<PropertyInfo>(v));

            foreach (var sourceProperty in sourceProperties)
            {
                var propertyType = normalizeType(sourceProperty.PropertyType);

                if (targetPropertiesByType.TryGetValue(propertyType, out Queue<PropertyInfo>? targetProperties))
                {
                    var mapping = new EntityPropertyMapping(
                        sourceProperty,
                        targetProperties.Dequeue(),
                        propertyType
                        );

                    mappings.Add(mapping);
                }
                else
                {
                    throw new NotSupportedException($"{sourceProperty.PropertyType.FullName} is not supported.");
                }
            }

            MappingCache.TryAdd(sourceType, mappings);

            return mappings;

            static Type normalizeType(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings<T>()
        {
            return GetMappings(typeof(T));
        }
    }
}