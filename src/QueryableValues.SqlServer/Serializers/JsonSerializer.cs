using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BlazarTech.QueryableValues.Serializers
{
    internal sealed class JsonSerializer : IJsonSerializer
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();
        private static readonly JsonEncodedText IndexPropertyName = JsonEncodedText.Encode(QueryableValuesEntity.IndexPropertyName);

        private static string SerializePrivate<T>(T values)
        {
            return System.Text.Json.JsonSerializer.Serialize(values);
        }

        public string Serialize(IEnumerable<byte> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<short> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<int> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<long> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<decimal> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<float> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<double> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<DateTime> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<DateTimeOffset> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<Guid> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<char> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize(IEnumerable<string> values)
        {
            return SerializePrivate(values);
        }

        public string Serialize<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
            where T : notnull
        {
            var properties = new PropertyWriter[propertyMappings.Count];

            for (var i = 0; i < properties.Length; i++)
            {
                properties[i] = new PropertyWriter(propertyMappings[i]);
            }

            void writeEntity(Utf8JsonWriter writer, T entity)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i].WriteValue(writer, entity);
                }
            }

            static bool mustSkipValue(T v) => v is null;

            return GetJson(values, writeEntity, mustSkipValue);

            static string GetJson(IEnumerable<T> values, Action<Utf8JsonWriter, T> writeValue, Func<T, bool>? mustSkipValue = null)
            {
                using var stream = (RecyclableMemoryStream)MemoryStreamManager.GetStream();

                using (var jsonWriter = new Utf8JsonWriter((IBufferWriter<byte>)stream))
                {
                    jsonWriter.WriteStartArray();

                    var index = 0;

                    foreach (var value in values)
                    {
                        if (mustSkipValue?.Invoke(value) == true)
                        {
                            continue;
                        }

                        jsonWriter.WriteStartObject();

                        jsonWriter.WritePropertyName(IndexPropertyName);
                        jsonWriter.WriteNumberValue(index++);

                        writeValue(jsonWriter, value);

                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndArray();
                }

#if NETSTANDARD2_0
                var streamInt32Length = (int)stream.Length;
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, streamInt32Length);
#elif NETSTANDARD2_1_OR_GREATER
                stream.Position = 0;
                var streamInt32Length = (int)stream.Length;
                var span = stream.GetSpan();

                if (span.Length >= streamInt32Length)
                {
                    return Encoding.UTF8.GetString(span[..streamInt32Length]);
                }
                else
                {
                    return Encoding.UTF8.GetString(stream.GetBuffer(), 0, streamInt32Length);
                }
#else
                return Encoding.UTF8.GetString(stream.GetReadOnlySequence());
#endif
            }
        }

        private sealed class PropertyWriter
        {
            private static readonly Action<Utf8JsonWriter, bool> WriteBool = (Utf8JsonWriter writer, bool value) => writer.WriteBooleanValue(value);
            private static readonly Action<Utf8JsonWriter, byte> WriteByte = (Utf8JsonWriter writer, byte value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, short> WriteInt16 = (Utf8JsonWriter writer, short value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, int> WriteInt32 = (Utf8JsonWriter writer, int value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, long> WriteInt64 = (Utf8JsonWriter writer, long value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, decimal> WriteDecimal = (Utf8JsonWriter writer, decimal value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, float> WriteSingle = (Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
            private static readonly Action<Utf8JsonWriter, double> WriteDouble = (Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);

            private static readonly Action<Utf8JsonWriter, DateTime> WriteDateTime = (Utf8JsonWriter writer, DateTime value) =>
            {
                if (value.Kind != DateTimeKind.Unspecified)
                {
                    writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Unspecified));
                }
                else
                {
                    writer.WriteStringValue(value);
                }
            };

            private static readonly Action<Utf8JsonWriter, DateTimeOffset> WriteDateTimeOffset = (Utf8JsonWriter writer, DateTimeOffset value) => writer.WriteStringValue(value);
            private static readonly Action<Utf8JsonWriter, Guid> WriteGuid = (Utf8JsonWriter writer, Guid value) => writer.WriteStringValue(value);
            private static readonly Action<Utf8JsonWriter, char> WriteChar = (Utf8JsonWriter writer, char value) => writer.WriteStringValue(stackalloc[] { value });

            private readonly string _targetName;
            private readonly Action<Utf8JsonWriter, object?>? _writeValue;

            public EntityPropertyMapping Mapping { get; }

            public PropertyWriter(EntityPropertyMapping mapping)
            {
                Mapping = mapping;

                _targetName = mapping.Target.Name;

                _writeValue = mapping.TypeName switch
                {
                    EntityPropertyTypeName.Boolean => (writer, value) => WriteAttribute(writer, (bool?)value, WriteBool),
                    EntityPropertyTypeName.Byte => (writer, value) => WriteAttribute(writer, (byte?)value, WriteByte),
                    EntityPropertyTypeName.Int16 => (writer, value) => WriteAttribute(writer, (short?)value, WriteInt16),
                    EntityPropertyTypeName.Int32 => (writer, value) => WriteAttribute(writer, (int?)value, WriteInt32),
                    EntityPropertyTypeName.Int64 => (writer, value) => WriteAttribute(writer, (long?)value, WriteInt64),
                    EntityPropertyTypeName.Decimal => (writer, value) => WriteAttribute(writer, (decimal?)value, WriteDecimal),
                    EntityPropertyTypeName.Single => (writer, value) => WriteAttribute(writer, (float?)value, WriteSingle),
                    EntityPropertyTypeName.Double => (writer, value) => WriteAttribute(writer, (double?)value, WriteDouble),
                    EntityPropertyTypeName.DateTime => (writer, value) => WriteAttribute(writer, (DateTime?)value, WriteDateTime),
                    EntityPropertyTypeName.DateTimeOffset => (writer, value) => WriteAttribute(writer, (DateTimeOffset?)value, WriteDateTimeOffset),
                    EntityPropertyTypeName.Guid => (writer, value) => WriteAttribute(writer, (Guid?)value, WriteGuid),
                    EntityPropertyTypeName.Char => (writer, value) => WriteAttribute(writer, (char?)value, WriteChar),
                    EntityPropertyTypeName.String => (writer, value) => WriteStringAttribute(writer, (string?)value),
                    _ => throw new NotImplementedException(mapping.TypeName.ToString()),
                };
            }

            private void WriteAttribute<TValue>(Utf8JsonWriter writer, TValue? value, Action<Utf8JsonWriter, TValue> writeValue)
                where TValue : struct
            {
                if (value.HasValue)
                {
                    writer.WritePropertyName(_targetName);
                    writeValue(writer, value.Value);
                }
            }

            private void WriteStringAttribute(Utf8JsonWriter writer, string? value)
            {
                if (value != null)
                {
                    writer.WritePropertyName(_targetName);
                    writer.WriteStringValue(value);
                }
            }

            public void WriteValue(Utf8JsonWriter writer, object entity)
            {
                if (_writeValue != null)
                {
                    var value = Mapping.Source.GetValue(entity);
                    _writeValue.Invoke(writer, value);
                }
            }
        }
    }
}
