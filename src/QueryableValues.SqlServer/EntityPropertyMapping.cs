using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace BlazarTech.QueryableValues
{
    internal sealed class EntityPropertyMapping
    {
        internal static readonly IReadOnlyDictionary<Type, EntityPropertyTypeName> SimpleTypes;

        private static readonly PropertyInfo[] EntityProperties = typeof(QueryableValuesEntity).GetProperties().Where(i => i.Name != QueryableValuesEntity.IndexPropertyName).ToArray();
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<EntityPropertyMapping>> MappingCache = new ConcurrentDictionary<Type, IReadOnlyList<EntityPropertyMapping>>();

        public PropertyInfo Source { get; }
        public PropertyInfo Target { get; }
        public Type NormalizedType { get; }
        public EntityPropertyTypeName TypeName { get; }
        public bool IsSourceEnum { get; }

        static EntityPropertyMapping()
        {
            SimpleTypes = new Dictionary<Type, EntityPropertyTypeName>
            {
                { typeof(bool), EntityPropertyTypeName.Boolean },
                { typeof(byte), EntityPropertyTypeName.Byte },
                { typeof(short), EntityPropertyTypeName.Int16 },
                { typeof(int), EntityPropertyTypeName.Int32 },
                { typeof(long), EntityPropertyTypeName.Int64 },
                { typeof(decimal), EntityPropertyTypeName.Decimal },
                { typeof(float), EntityPropertyTypeName.Single },
                { typeof(double), EntityPropertyTypeName.Double },
                { typeof(DateTime), EntityPropertyTypeName.DateTime },
                { typeof(DateTimeOffset), EntityPropertyTypeName.DateTimeOffset },
                { typeof(Guid), EntityPropertyTypeName.Guid },
                { typeof(char), EntityPropertyTypeName.Char },
                { typeof(string), EntityPropertyTypeName.String }
            };
        }

        private EntityPropertyMapping(PropertyInfo source, PropertyInfo target, Type normalizedType, bool isSourceEnum)
        {
            Source = source;
            Target = target;
            NormalizedType = normalizedType;
            TypeName = GetTypeName(normalizedType);
            IsSourceEnum = isSourceEnum;

            if (TypeName == EntityPropertyTypeName.Unknown)
            {
                throw new NotSupportedException($"{source.PropertyType.FullName} is not supported.");
            }
        }

        public static EntityPropertyTypeName GetTypeName(Type type)
        {
            if (SimpleTypes.TryGetValue(type, out EntityPropertyTypeName typeName))
            {
                return typeName;
            }
            else
            {
                return EntityPropertyTypeName.Unknown;
            }
        }

        public static Type GetNormalizedType(Type type, out bool isEnum)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            isEnum = type.IsEnum;

            if (isEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            return type;
        }

        public static Type GetNormalizedType(Type type) => GetNormalizedType(type, out _);

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
                var sourcePropertyNormalizedType = GetNormalizedType(sourceProperty.PropertyType, out var isSourceEnum);

                if (targetPropertiesByType.TryGetValue(sourcePropertyNormalizedType, out Queue<PropertyInfo>? targetProperties))
                {
                    if (targetProperties.Count == 0)
                    {
                        throw new InvalidOperationException($"Mapping properties for the type {sourceProperty.PropertyType.FullName} have been depleted.");
                    }

                    var mapping = new EntityPropertyMapping(
                        sourceProperty,
                        targetProperties.Dequeue(),
                        sourcePropertyNormalizedType,
                        isSourceEnum
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

        public object? GetSourceNormalizedValue(object objectInstance)
        {
            var value = Source.GetValue(objectInstance);

            if (value is null)
            {
                return null;
            }

            if (IsSourceEnum)
            {
                switch (TypeName)
                {
                    case EntityPropertyTypeName.Int32:
                        value = (int)value;
                        break;
                    case EntityPropertyTypeName.Byte:
                        value = (byte)value;
                        break;
                    case EntityPropertyTypeName.Int16:
                        value = (short)value;
                        break;
                    case EntityPropertyTypeName.Int64:
                        value = (long)value;
                        break;
                    default:
                        throw new NotSupportedException($"The underlying type of {NormalizedType.FullName} ({Enum.GetUnderlyingType(NormalizedType).FullName}) is not supported.");
                }
            }

            return value;
        }
    }
}