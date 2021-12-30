using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BlazarTech.QueryableValues
{
    internal static class XmlUtil
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

        /*
            Discovered bug when serializing char/string.
            Some characters must be entitized so SQL Server can properly represent them.
            System.Xml.XmlWriter is not flexible on this.
            todo: Use a combination of xmlWriter.WriteCharEntity and xmlWriter.WriteChars when writing the content of a char/string.
            Related to: https://github.com/yv989c/BlazarTech.QueryableValues/issues/4
         */

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
        private static void WriteValue(XmlWriter writer, char value) => writer.WriteValue(value.ToString());
        private static void WriteValue(XmlWriter writer, string value) => writer.WriteValue(value);

        public static string GetXml(IEnumerable<byte> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<short> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<int> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<long> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<decimal> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<float> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<double> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<DateTime> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<DateTimeOffset> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<Guid> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<char> values)
        {
            return GetXml(values, WriteValue);
        }

        public static string GetXml(IEnumerable<string> values)
        {
            static bool mustSkipValue(string v) => v is null;
            return GetXml(values, WriteValue, mustSkipValue);
        }

        public static string GetXml<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
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
                    EntityPropertyTypeName.Boolean => (writer, value) => WriteAttribute(writer, (bool?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Byte => (writer, value) => WriteAttribute(writer, (byte?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Int16 => (writer, value) => WriteAttribute(writer, (short?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Int32 => (writer, value) => WriteAttribute(writer, (int?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Int64 => (writer, value) => WriteAttribute(writer, (long?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Decimal => (writer, value) => WriteAttribute(writer, (decimal?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Single => (writer, value) => WriteAttribute(writer, (float?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Double => (writer, value) => WriteAttribute(writer, (double?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.DateTime => (writer, value) => WriteAttribute(writer, (DateTime?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.DateTimeOffset => (writer, value) => WriteAttribute(writer, (DateTimeOffset?)value, XmlUtil.WriteValue),
                    EntityPropertyTypeName.Guid => (writer, value) => WriteAttribute(writer, (Guid?)value, XmlUtil.WriteValue),
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
                    XmlUtil.WriteValue(writer, value);
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