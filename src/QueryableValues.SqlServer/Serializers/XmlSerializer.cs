﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BlazarTech.QueryableValues.Serializers
{
    internal sealed class XmlSerializer : IXmlSerializer
    {
        private static string GetXml<T>(IEnumerable<T> values, Action<XmlWriter, T> writeValue, Func<T, bool>? mustSkipValue = null)
        {
            var sb = new StringBuilder();

            using (var stringWriter = new System.IO.StringWriter(sb))
            {
                var settings = new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment
                };

                using var xmlWriter = XmlWriter.Create(stringWriter, settings);

                xmlWriter.WriteStartElement("R");

                foreach (var value in values)
                {
                    if (mustSkipValue?.Invoke(value) == true)
                    {
                        continue;
                    }

                    xmlWriter.WriteStartElement("V");

                    writeValue(xmlWriter, value);

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
            }

            return sb.ToString();
        }

        private static void WriteValue(XmlWriter writer, bool value) => writer.WriteValue(value ? 1 : 0);
        private static void WriteValue(XmlWriter writer, byte value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, short value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, int value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, long value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, decimal value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, float value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, double value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, DateTime value)
        {
            if (value.Kind != DateTimeKind.Unspecified)
            {
                writer.WriteValue(DateTime.SpecifyKind(value, DateTimeKind.Unspecified));
            }
            else
            {
                writer.WriteValue(value);
            }
        }
        private static void WriteValue(XmlWriter writer, DateTimeOffset value) => writer.WriteValue(value);
        private static void WriteValue(XmlWriter writer, Guid value) => writer.WriteValue(value.ToString());
        private static void WriteValue(XmlWriter writer, char[] chars)
        {
            var startIndex = 0;
            var length = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                var isValidCharacter = XmlConvert.IsXmlChar(c);
                var mustEntitize = isValidCharacter && (char.IsWhiteSpace(c) || char.IsControl(c));

                if (mustEntitize)
                {
                    WriterHelper(writer, chars, startIndex, ref length);
                    writer.WriteCharEntity(c);
                    startIndex = i + 1;
                }
                else if (isValidCharacter)
                {
                    length++;
                }
                else if (
                    i + 1 < chars.Length &&
                    // todo: Do I have to worry about endianness here?
                    XmlConvert.IsXmlSurrogatePair(chars[i + 1], chars[i])
                    )
                {
                    length += 2;
                    i++;
                }
                // It is an illegal XML character.
                // https://www.w3.org/TR/xml/#charsets
                else
                {
                    length++;
                    chars[i] = '?';
                }
            }

            WriterHelper(writer, chars, startIndex, ref length);

            static void WriterHelper(XmlWriter writer, char[] chars, int startIndex, ref int length)
            {
                if (length > 0)
                {
                    writer.WriteChars(chars, startIndex, length);
                    length = 0;
                }
            }
        }
        private static void WriteValue(XmlWriter writer, char value) => WriteValue(writer, new[] { value });
        private static void WriteValue(XmlWriter writer, string value) => WriteValue(writer, value.ToCharArray());

        public string Serialize(IEnumerable<byte> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<short> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<int> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<long> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<decimal> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<float> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<double> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<DateTime> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<DateTimeOffset> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<Guid> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<char> values)
        {
            return GetXml(values, WriteValue);
        }

        public string Serialize(IEnumerable<string> values)
        {
            static bool mustSkipValue(string v) => v is null;
            return GetXml(values, WriteValue, mustSkipValue);
        }

        public string Serialize<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
            where T : notnull
        {
            var properties = new PropertyWriter[propertyMappings.Count];

            for (int i = 0; i < properties.Length; i++)
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
        }

        private sealed class PropertyWriter
        {
            private readonly string _targetName;
            private readonly Action<XmlWriter, object?>? _writeValue;

            public EntityPropertyMapping Mapping { get; }

            public PropertyWriter(EntityPropertyMapping mapping)
            {
                Mapping = mapping;

                _targetName = mapping.Target.Name;

                _writeValue = mapping.TypeName switch
                {
                    EntityPropertyTypeName.Boolean => (writer, value) => WriteAttribute(writer, (bool?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Byte => (writer, value) => WriteAttribute(writer, (byte?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Int16 => (writer, value) => WriteAttribute(writer, (short?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Int32 => (writer, value) => WriteAttribute(writer, (int?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Int64 => (writer, value) => WriteAttribute(writer, (long?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Decimal => (writer, value) => WriteAttribute(writer, (decimal?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Single => (writer, value) => WriteAttribute(writer, (float?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Double => (writer, value) => WriteAttribute(writer, (double?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.DateTime => (writer, value) => WriteAttribute(writer, (DateTime?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.DateTimeOffset => (writer, value) => WriteAttribute(writer, (DateTimeOffset?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Guid => (writer, value) => WriteAttribute(writer, (Guid?)value, XmlSerializer.WriteValue),
                    EntityPropertyTypeName.Char => (writer, value) => WriteAttribute(writer, (char?)value, XmlSerializer.WriteValue),
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
                    XmlSerializer.WriteValue(writer, value);
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