using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BlazarTech.QueryableValues.Serializers
{
    internal sealed class XmlSerializer : IXmlSerializer
    {
        private static readonly DefaultObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPool<StringBuilder>(
            new StringBuilderPooledObjectPolicy
            {
                InitialCapacity = 512,
                MaximumRetainedCapacity = 524288
            });

        private static readonly ArrayPool<char> BufferPool = ArrayPool<char>.Shared;

        private sealed class WriterHelper : IDisposable
        {
            public readonly StringBuilder Sb;
            public readonly char[] Buffer;

            public WriterHelper(int bufferLength)
            {
                Sb = StringBuilderPool.Get();
                Buffer = bufferLength > 0 ? BufferPool.Rent(bufferLength) : Array.Empty<char>();
            }

            public void Dispose()
            {
                if (Buffer.Length > 0)
                {
                    BufferPool.Return(Buffer);
                }

                StringBuilderPool.Return(Sb);
            }
        }

        private static void WriteValue(XmlWriter writer, char[] chars, int length)
        {
            var startIndex = 0;
            var localLength = 0;

            for (int i = 0; i < length; i++)
            {
                var c = chars[i];
                var isValidCharacter = XmlConvert.IsXmlChar(c);
                var mustEntitize = isValidCharacter && (char.IsWhiteSpace(c) || char.IsControl(c));

                if (mustEntitize)
                {
                    WriterHelper(writer, chars, startIndex, ref localLength);
                    writer.WriteCharEntity(c);
                    startIndex = i + 1;
                }
                else if (isValidCharacter)
                {
                    localLength++;
                }
                else if (
                    i + 1 < length &&
                    // todo: Do I have to worry about endianness here?
                    XmlConvert.IsXmlSurrogatePair(chars[i + 1], chars[i])
                    )
                {
                    localLength += 2;
                    i++;
                }
                // It is an illegal XML character.
                // https://www.w3.org/TR/xml/#charsets
                else
                {
                    localLength++;
                    chars[i] = '?';
                }
            }

            WriterHelper(writer, chars, startIndex, ref localLength);

            static void WriterHelper(XmlWriter writer, char[] chars, int startIndex, ref int length)
            {
                if (length > 0)
                {
                    writer.WriteChars(chars, startIndex, length);
                    length = 0;
                }
            }
        }

        private static XmlWriter CreateXmlWriter(StringBuilder sb)
        {
            var settings = new XmlWriterSettings
            {
                CheckCharacters = false,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            return XmlWriter.Create(sb, settings);
        }

        public string Serialize<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
            where T : notnull
        {
            var properties = new PropertyWriter[propertyMappings.Count];

            for (var i = 0; i < properties.Length; i++)
            {
                properties[i] = new PropertyWriter(propertyMappings[i]);
            }

            void writeEntity(XmlWriter writer, T entity)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i].WriteValue(writer, entity);
                }
            }

            static bool mustSkipValue(T v) => v is null;

            return GetXml(values, writeEntity, mustSkipValue);

            static string GetXml(IEnumerable<T> values, Action<XmlWriter, T> writeValue, Func<T, bool>? mustSkipValue = null)
            {
                using var writer = new WriterHelper(0);

                using (var xmlWriter = CreateXmlWriter(writer.Sb))
                {
                    xmlWriter.WriteStartElement("R");

                    var index = 0;

                    foreach (var value in values)
                    {
                        if (mustSkipValue?.Invoke(value) == true)
                        {
                            continue;
                        }

                        xmlWriter.WriteStartElement("V");

                        xmlWriter.WriteStartAttribute(QueryableValuesEntity.IndexPropertyName);
                        xmlWriter.WriteValue(index++);
                        xmlWriter.WriteEndAttribute();

                        writeValue(xmlWriter, value);

                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }

                return writer.Sb.ToString();
            }
        }

        private sealed class PropertyWriter
        {
            private static readonly Action<XmlWriter, bool> WriteBool = (XmlWriter writer, bool value) => writer.WriteValue(value ? 1 : 0);
            private static readonly Action<XmlWriter, byte> WriteByte = (XmlWriter writer, byte value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, short> WriteInt16 = (XmlWriter writer, short value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, int> WriteInt32 = (XmlWriter writer, int value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, long> WriteInt64 = (XmlWriter writer, long value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, decimal> WriteDecimal = (XmlWriter writer, decimal value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, float> WriteSingle = (XmlWriter writer, float value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, double> WriteDouble = (XmlWriter writer, double value) => writer.WriteValue(value);

            private static readonly Action<XmlWriter, DateTime> WriteDateTime = (XmlWriter writer, DateTime value) =>
            {
                if (value.Kind != DateTimeKind.Unspecified)
                {
                    writer.WriteValue(DateTime.SpecifyKind(value, DateTimeKind.Unspecified));
                }
                else
                {
                    writer.WriteValue(value);
                }
            };

            private static readonly Action<XmlWriter, DateTimeOffset> WriteDateTimeOffset = (XmlWriter writer, DateTimeOffset value) => writer.WriteValue(value);
            private static readonly Action<XmlWriter, Guid> WriteGuid = (XmlWriter writer, Guid value) => writer.WriteValue(value.ToString());
            private static readonly Action<XmlWriter, char> WriteChar = (XmlWriter writer, char value) => XmlSerializer.WriteValue(writer, new[] { value }, 1);
            private static readonly Action<XmlWriter, string> WriteString = (XmlWriter writer, string value) => XmlSerializer.WriteValue(writer, value.ToCharArray(), value.Length);

            private readonly string _targetName;
            private readonly Action<XmlWriter, object?>? _writeValue;

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

            private void WriteAttribute<TValue>(XmlWriter writer, TValue? value, Action<XmlWriter, TValue> writeValue)
                where TValue : struct
            {
                if (value.HasValue)
                {
                    writer.WriteStartAttribute(_targetName);
                    writeValue(writer, value.Value);
                    writer.WriteEndAttribute();
                }
            }

            private void WriteStringAttribute(XmlWriter writer, string? value)
            {
                if (value != null)
                {
                    writer.WriteStartAttribute(_targetName);
                    WriteString(writer, value);
                    writer.WriteEndAttribute();
                }
            }

            public void WriteValue(XmlWriter writer, object entity)
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
