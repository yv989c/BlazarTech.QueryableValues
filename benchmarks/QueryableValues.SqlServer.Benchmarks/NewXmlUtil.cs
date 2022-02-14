using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QueryableValues.SqlServer.Benchmarks
{
    internal static class NewXmlUtil
    {
        private static string GetXml<T>(IEnumerable<T> values, Action<StringBuilder, T, char[]?> writeValue, Func<T, bool>? mustSkipValue = null, int bufferLength = 0, int valueLength = 20)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = ((valueLength + 7) * count) + 7;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");

            var buffer = bufferLength > 0 ? new char[bufferLength] : null;

            foreach (var value in values)
            {
                if (mustSkipValue?.Invoke(value) == true)
                {
                    continue;
                }

                sb.Append("<V>");
                writeValue(sb, value, buffer);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, bool value, char[]? buffer) => sb.Append(value ? 1 : 0);
        private static void WriteValue(StringBuilder sb, byte value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, short value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, int value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, long value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, decimal value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, float value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, double value, char[]? buffer) => sb.Append(value);
        private static void WriteValue(StringBuilder sb, DateTime value, char[]? buffer)
        {
            if (value.Kind != DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }

            sb.Append(XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind));
        }
        private static void WriteValue(StringBuilder sb, DateTimeOffset value, char[]? buffer)
        {
            if (value.Offset != TimeSpan.Zero)
            {
                sb.Append(XmlConvert.ToString(value.LocalDateTime, XmlDateTimeSerializationMode.RoundtripKind));
            }
            else
            {
                sb.Append(XmlConvert.ToString(value.UtcDateTime, XmlDateTimeSerializationMode.RoundtripKind));
            }
        }
        private static void WriteValue(StringBuilder sb, Guid value, char[]? buffer)
        {
            value.TryFormat(buffer, out _);
            sb.Append(buffer);
        }

        private static void WriteValue(StringBuilder sb, ReadOnlySpan<char> chars, bool forAttribute = false)
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
                    sb.Append("&#x");
                    var entityHex = ((int)c).ToString("X", NumberFormatInfo.InvariantInfo);
                    sb.Append(entityHex);
                    sb.Append(';');

                    //WriterHelper(writer, chars, startIndex, ref length);
                    //writer.WriteCharEntity(c);
                    startIndex = i + 1;
                }
                else if (isValidCharacter)
                {
                    switch (c)
                    {
                        case '<':
                            writeEntityRefImpl(sb, "lt");
                            break;
                        case '>':
                            writeEntityRefImpl(sb, "gt");
                            break;
                        case '&':
                            writeEntityRefImpl(sb, "amp");
                            break;
                        case '\'':
                            if (forAttribute)// && _quoteChar == ch)
                            {
                                writeEntityRefImpl(sb, "apos");
                            }
                            else
                            {
                                sb.Append('\'');
                            }
                            break;
                        case '"':
                            if (forAttribute)// && _quoteChar == ch)
                            {
                                writeEntityRefImpl(sb, "quot");
                            }
                            else
                            {
                                sb.Append('"');
                            }
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                    length++;
                }
                else if (
                    i + 1 < chars.Length &&
                    XmlConvert.IsXmlSurrogatePair(chars[i + 1], chars[i])
                    )
                {
                    var surrogateChar = combineSurrogateChar(chars[i + 1], chars[i]);
                    sb.Append("&#x");

                    //todo: https://stackoverflow.com/a/58810855/2206145
                    //surrogateChar.TryFormat
                    sb.Append(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo));

                    sb.Append(';');

                    //length += 2;
                    i++;
                }
                // It is an illegal XML character.
                // https://www.w3.org/TR/xml/#charsets
                else
                {
                    i++;
                    sb.Append('?');

                    //length++;
                    //chars[i] = '?';
                }
            }

            //WriterHelper(writer, chars, startIndex, ref length);

            //static void WriterHelper(XmlWriter writer, char[] chars, int startIndex, ref int length)
            //{
            //    if (length > 0)
            //    {
            //        writer.WriteChars(chars, startIndex, length);
            //        length = 0;
            //    }
            //}

            static void writeEntityRefImpl(StringBuilder sb, string name)
            {
                sb.Append('&');
                sb.Append(name);
                sb.Append(';');
            }

            const int SurLowStart = 0xdc00;    // 1101 11xx
            const int SurHighStart = 0xd800;    // 1101 10xx

            static int combineSurrogateChar(int lowChar, int highChar)
            {
                return (lowChar - SurLowStart) | ((highChar - SurHighStart) << 10) + 0x10000;
            }
        }
        // todo: do not use array.
        private static void WriteValue(StringBuilder sb, char value, char[]? buffer) => WriteValue(sb, new[] { value });
        private static void WriteValue(StringBuilder sb, string value, char[]? buffer) => WriteValue(sb, value.AsSpan());

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

        public static string GetXml2(IEnumerable<int> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");

            var s = new string('\0', 11);
            var writeable = MemoryMarshal.AsMemory(s.AsMemory()).Span;

            foreach (var value in values)
            {
                if (value.TryFormat(writeable, out int charsWriten))
                {
                    sb.Append("<V>");
                    sb.Append(writeable[..charsWriten]);
                    sb.Append("</V>");
                }
                else
                {
                    throw new Exception("Impossible");
                }
            }

            sb.Append("</R>");

            return sb.ToString();
            //return GetXml(values, WriteValue);
        }

        public static string GetXml3(IEnumerable<int> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");

            var v1 = "<V>".AsSpan();
            var v2 = "</V>".AsSpan();
            var s = new string('\0', 11);
            var writeable = MemoryMarshal.AsMemory(s.AsMemory()).Span;

            foreach (var value in values)
            {
                if (value.TryFormat(writeable, out int charsWriten))
                {
                    sb.Append(v1);
                    sb.Append(writeable[..charsWriten]);
                    sb.Append(v2);
                }
                else
                {
                    throw new Exception("Impossible");
                }
            }

            sb.Append("</R>");

            return sb.ToString();
            //return GetXml(values, WriteValue);
        }


        public static string GetXml4(IEnumerable<int> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");

            foreach (var value in values)
            {
                sb.Append("<V>");
                sb.Append(value);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }


        public static string GetXml5(IEnumerable<int> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");
            var chars = new char[11];

            foreach (var value in values)
            {
                sb.Append("<V>");
                value.TryFormat(chars, out int charsWritten);
                sb.Append(chars[..charsWritten]);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }


        public static string GetXml6(IEnumerable<int> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");
            var chars = new char[11].AsSpan();

            foreach (var value in values)
            {
                sb.Append("<V>");
                value.TryFormat(chars, out int charsWritten);
                sb.Append(chars[..charsWritten]);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
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
            return GetXml(values, WriteValue, bufferLength: 36, valueLength: 36);
        }

        // good
        public static string GetXmlGuid2(IEnumerable<Guid> values)
        {
            //var capacity = 100;

            //if (values.TryGetNonEnumeratedCount(out int count))
            //{
            //    capacity = count * 20;
            //}

            var sb = new StringBuilder();

            sb.Append("<R>");
            var span = new char[36];
            foreach (var value in values)
            {
                sb.Append("<V>");
                value.TryFormat(span, out _);
                sb.Append(span);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }


        public static string GetXmlGuid3(IEnumerable<Guid> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");
            var span = new char[36].AsSpan();
            foreach (var value in values)
            {
                sb.Append("<V>");
                value.TryFormat(span, out _);
                sb.Append(span);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }

        public static string GetXmlGuid4(IEnumerable<Guid> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");

            var s = new string('\0', 36);
            var writeable = MemoryMarshal.AsMemory(s.AsMemory()).Span;

            foreach (var value in values)
            {
                sb.Append("<V>");
                value.TryFormat(writeable, out _);
                sb.Append(writeable);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
        }


        public static string GetXmlGuid5(IEnumerable<Guid> values)
        {
            var capacity = 100;

            if (values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count * 20;
            }

            var sb = new StringBuilder(capacity);

            sb.Append("<R>");
            foreach (var value in values)
            {
                sb.Append("<V>");
                var span = new char[36];
                value.TryFormat(span, out _);
                sb.Append(span);
                sb.Append("</V>");
            }

            sb.Append("</R>");

            return sb.ToString();
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
