using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazarTech.QueryableValues
{
    internal sealed class EntityPropertyMapping
    {
        private static readonly PropertyInfo[] EntityProperties = typeof(QueryableValuesEntity).GetProperties();

        public PropertyInfo Source { get; }
        public PropertyInfo Target { get; }
        public Type NormalizedType { get; }

        private EntityPropertyMapping(PropertyInfo source, PropertyInfo target, Type normalizedType)
        {
            Source = source;
            Target = target;
            NormalizedType = normalizedType;
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings(Type sourceType)
        {
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

            return mappings;

            static Type normalizeType(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        }

        public static IReadOnlyList<EntityPropertyMapping> GetMappings<T>()
        {
            return GetMappings(typeof(T));
        }
    }
}