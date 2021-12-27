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
        private static readonly Dictionary<Type, EntityPropertyTypeName> SimpleTypes;

        public PropertyInfo Source { get; }
        public PropertyInfo Target { get; }
        public Type NormalizedType { get; }
        public EntityPropertyTypeName TypeName { get; }

        static EntityPropertyMapping()
        {
            SimpleTypes = new Dictionary<Type, EntityPropertyTypeName>
            {
                { typeof(int), EntityPropertyTypeName.Int },
                { typeof(long), EntityPropertyTypeName.Long },
                { typeof(decimal), EntityPropertyTypeName.Decimal },
                { typeof(double), EntityPropertyTypeName.Double },
                { typeof(DateTime), EntityPropertyTypeName.DateTime },
                { typeof(DateTimeOffset), EntityPropertyTypeName.DateTimeOffset },
                { typeof(Guid), EntityPropertyTypeName.Guid },
                { typeof(string), EntityPropertyTypeName.String }
            };
        }

        private EntityPropertyMapping(PropertyInfo source, PropertyInfo target, Type normalizedType)
        {
            Source = source;
            Target = target;
            NormalizedType = normalizedType;

            if (SimpleTypes.TryGetValue(normalizedType, out EntityPropertyTypeName typeName))
            {
                TypeName = typeName;
            }
            else
            {
                throw new NotSupportedException($"{source.PropertyType.FullName} is not supported.");
            }
        }

        private static Type GetNormalizedType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        public static bool IsSimpleType(Type type)
        {
            var normalizedType = GetNormalizedType(type);
            return SimpleTypes.ContainsKey(normalizedType);
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings(Type sourceType)
        {
            if (MappingCache.TryGetValue(sourceType, out IReadOnlyList<EntityPropertyMapping>? mappingsFromCache))
            {
                return mappingsFromCache;
            }

            var sourceProperties = sourceType.GetProperties();

            if (sourceProperties.Length == 0)
            {
                throw new InvalidOperationException($"The type {sourceType.FullName} must have at least one public property.");
            }

            var mappings = new List<EntityPropertyMapping>(sourceProperties.Length);

            var targetPropertiesByType = (
                from i in EntityProperties
                group i by GetNormalizedType(i.PropertyType) into g
                select g
                )
                .ToDictionary(k => k.Key, v => new Queue<PropertyInfo>(v));

            foreach (var sourceProperty in sourceProperties)
            {
                var propertyType = GetNormalizedType(sourceProperty.PropertyType);

                if (targetPropertiesByType.TryGetValue(propertyType, out Queue<PropertyInfo>? targetProperties))
                {
                    if (targetProperties.Count == 0)
                    {
                        throw new InvalidOperationException($"Mapping properties for the type {sourceProperty.PropertyType.FullName} have been depleted.");
                    }

                    var mapping = new EntityPropertyMapping(
                        sourceProperty,
                        targetProperties.Dequeue(),
                        propertyType
                        );

                    mappings.Add(mapping);
                }
                else
                {
                    throw new NotSupportedException($"The type {sourceProperty.PropertyType.FullName} on the {sourceProperty.Name} property is not supported.");
                }
            }

            MappingCache.TryAdd(sourceType, mappings);

            return mappings;
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings<T>()
        {
            return GetMappings(typeof(T));
        }
    }
}