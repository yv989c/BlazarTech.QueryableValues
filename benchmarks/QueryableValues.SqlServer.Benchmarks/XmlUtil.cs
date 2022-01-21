using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QueryableValues.SqlServer.Benchmarks
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
                    ConformanceLevel = ConformanceLevel.Fragment,
                    CheckCharacters = false
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
    }
}
