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

        private static void WriteValue(XmlWriter writer, int v) => writer.WriteValue(v);
        private static void WriteValue(XmlWriter writer, long v) => writer.WriteValue(v);
        private static void WriteValue(XmlWriter writer, decimal v) => writer.WriteValue(v);
        private static void WriteValue(XmlWriter writer, double v) => writer.WriteValue(v);
        private static void WriteValue(XmlWriter writer, DateTime v) => writer.WriteValue(DateTime.SpecifyKind(v, DateTimeKind.Unspecified));
        private static void WriteValue(XmlWriter writer, DateTimeOffset v) => writer.WriteValue(v);
        private static void WriteValue(XmlWriter writer, Guid v) => writer.WriteValue(v.ToString());
        private static void WriteValue(XmlWriter writer, string v) => writer.WriteValue(v);

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

        public static string GetXml(IEnumerable<string> values)
        {
            static bool mustSkipValue(string v) => v is null;
            return GetXml(values, WriteValue, mustSkipValue);
        }

        public static string GetXml<T>(IEnumerable<T> values, EntityPropertyMapping[] propertyMappings)
            where T : notnull
        {
            var properties = new PropertyWriter[propertyMappings.Length];

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
            private static readonly Type IntType = typeof(int);
            private static readonly Type LongType = typeof(long);
            private static readonly Type DecimalType = typeof(decimal);
            private static readonly Type DoubleType = typeof(double);
            private static readonly Type DateTimeType = typeof(DateTime);
            private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
            private static readonly Type GuidType = typeof(Guid);
            private static readonly Type StringType = typeof(string);

            private readonly string _targetName;
            private readonly Action<XmlWriter, object>? _writeValue;

            public EntityPropertyMapping Mapping { get; }

            public PropertyWriter(EntityPropertyMapping mapping)
            {
                Mapping = mapping;

                _targetName = mapping.Target.Name;

                var type = mapping.NormalizedType;

                if (type == IntType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (int?)value, XmlUtil.WriteValue);
                }
                else if (type == LongType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (long?)value, XmlUtil.WriteValue);
                }
                else if (type == DecimalType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (decimal?)value, XmlUtil.WriteValue);
                }
                else if (type == DoubleType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (double?)value, XmlUtil.WriteValue);
                }
                else if (type == DateTimeType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (DateTime?)value, XmlUtil.WriteValue);
                }
                else if (type == DateTimeOffsetType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (DateTimeOffset?)value, XmlUtil.WriteValue);
                }
                else if (type == GuidType)
                {
                    _writeValue = (writer, value) => WriteAttribute(writer, (Guid?)value, XmlUtil.WriteValue);
                }
                else if (type == StringType)
                {
                    _writeValue = (writer, value) => WriteStringAttribute(writer, (string?)value);
                }
                else
                {
                    throw new NotSupportedException($"{mapping.Source.PropertyType.FullName} is not supported.");
                }
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