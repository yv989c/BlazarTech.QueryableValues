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

        public static string GetXml(IEnumerable<int> values)
        {
            static void writeValue(XmlWriter writer, int v) => writer.WriteValue(v);
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<long> values)
        {
            static void writeValue(XmlWriter writer, long v) => writer.WriteValue(v);
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<decimal> values)
        {
            static void writeValue(XmlWriter writer, decimal v) => writer.WriteValue(v);
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<double> values)
        {
            static void writeValue(XmlWriter writer, double v) => writer.WriteValue(v);
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<DateTime> values)
        {
            static void writeValue(XmlWriter writer, DateTime v) => writer.WriteValue(DateTime.SpecifyKind(v, DateTimeKind.Unspecified));
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<DateTimeOffset> values)
        {
            static void writeValue(XmlWriter writer, DateTimeOffset v) => writer.WriteValue(v);
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<Guid> values)
        {
            static void writeValue(XmlWriter writer, Guid v) => writer.WriteValue(v.ToString());
            return GetXml(values, writeValue);
        }

        public static string GetXml(IEnumerable<string> values)
        {
            static bool mustSkipValue(string v) => v is null;
            static void writeValue(XmlWriter writer, string v) => writer.WriteValue(v);
            return GetXml(values, writeValue, mustSkipValue);
        }
    }
}