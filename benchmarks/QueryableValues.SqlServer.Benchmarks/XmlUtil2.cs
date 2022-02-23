using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.ObjectPool;

namespace QueryableValues.SqlServer.Benchmarks
{
    internal class XmlUtil2
    {
        private const int ValueBufferSize = 128;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        private static readonly DefaultObjectPool<StringBuilder> StringBuilderPool = new(new StringBuilderPooledObjectPolicy
        {
            InitialCapacity = 100,
            MaximumRetainedCapacity = 100000
        });

        private static readonly ArrayPool<char> BufferPool = ArrayPool<char>.Shared;

        private static readonly Func<IEnumerable<byte>, string> GetXmlByte = (IEnumerable<byte> values) => GetXml(values, WriteValue, null, 3);
        private static readonly Func<IEnumerable<short>, string> GetXmlInt16 = (IEnumerable<short> values) => GetXml(values, WriteValue, null, 3);
        private static readonly Func<IEnumerable<int>, string> GetXmlInt32 = (IEnumerable<int> values) => GetXml(values, WriteValue, null, 5);
        private static readonly Func<IEnumerable<long>, string> GetXmlInt64 = (IEnumerable<long> values) => GetXml(values, WriteValue, null, 10);
        private static readonly Func<IEnumerable<decimal>, string> GetXmlDecimal = (IEnumerable<decimal> values) => GetXml(values, WriteValue, null, 10, true);
        private static readonly Func<IEnumerable<float>, string> GetXmlSingle = (IEnumerable<float> values) => GetXml(values, WriteValue, null, 10, true);
        private static readonly Func<IEnumerable<double>, string> GetXmlDouble = (IEnumerable<double> values) => GetXml(values, WriteValue, null, 10, true);
        private static readonly Func<IEnumerable<DateTime>, string> GetXmlDateTime = (IEnumerable<DateTime> values) => GetXml(values, WriteValue, null, 27, true);
        private static readonly Func<IEnumerable<DateTimeOffset>, string> GetXmlDateTimeOffset = (IEnumerable<DateTimeOffset> values) => GetXml(values, WriteValue, null, 33, true);
        private static readonly Func<IEnumerable<Guid>, string> GetXmlGuid = (IEnumerable<Guid> values) => GetXml(values, WriteValue, null, 36, true);

        private static void EnsureCapacity<T>(StringBuilder sb, IEnumerable<T> values, int valueMinLength)
        {
            if (valueMinLength > 0 && values.TryGetNonEnumeratedCount(out int count))
            {
                var capacity = ((valueMinLength + 7) * count) + 7;
                sb.EnsureCapacity(capacity);
            }
        }

        private static string GetXml<T>(
            IEnumerable<T> values,
            Action<T, StringBuilder, char[]> writeValue,
            Func<T, bool>? mustSkipValue = null,
            int valueMinLength = 0,
            bool useBuffer = false
            )
        {
            var sb = StringBuilderPool.Get();
            var buffer = useBuffer ? BufferPool.Rent(ValueBufferSize) : Array.Empty<char>();

            try
            {
                EnsureCapacity(sb, values, valueMinLength);

                sb.Append("<R>");

                foreach (var value in values)
                {
                    if (mustSkipValue?.Invoke(value) == true)
                    {
                        continue;
                    }

                    sb.Append("<V>");
                    writeValue(value, sb, buffer);
                    sb.Append("</V>");
                }

                sb.Append("</R>");

                return sb.ToString();
            }
            finally
            {
                if (useBuffer)
                {
                    BufferPool.Return(buffer);
                }

                StringBuilderPool.Return(sb);
            }
        }

        private static void WriteValue(bool value, StringBuilder sb, char[] buffer) => sb.Append(value ? '1' : '0');

        private static void WriteValue(byte value, StringBuilder sb, char[] buffer) => sb.Append(value);

        private static void WriteValue(short value, StringBuilder sb, char[] buffer) => sb.Append(value);

        private static void WriteValue(int value, StringBuilder sb, char[] buffer) => sb.Append(value);

        private static void WriteValue(long value, StringBuilder sb, char[] buffer) => sb.Append(value);

        private static void WriteValue(decimal value, StringBuilder sb, char[] buffer) => AppendSpanFormattable(value, sb, buffer);

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.Xml/src/System/Xml/XmlConvert.cs#L726
        private static void WriteValue(float value, StringBuilder sb, char[] buffer)
        {
            if (float.IsNegativeInfinity(value))
            {
                sb.Append("-INF");
            }
            else if (float.IsPositiveInfinity(value))
            {
                sb.Append("INF");
            }
            else if (IsNegativeZero(value))
            {
                sb.Append("-0");
            }
            else
            {
                AppendSpanFormattable(value, sb, buffer, "R");
            }
        }

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.Xml/src/System/Xml/XmlConvert.cs#L737
        private static void WriteValue(double value, StringBuilder sb, char[] buffer)
        {
            if (double.IsNegativeInfinity(value))
            {
                sb.Append("-INF");
            }
            else if (double.IsPositiveInfinity(value))
            {
                sb.Append("INF");
            }
            else if (IsNegativeZero(value))
            {
                sb.Append("-0");
            }
            else
            {
                AppendSpanFormattable(value, sb, buffer, "R");
            }
        }

        private static void WriteValue(DateTime value, StringBuilder sb, char[] buffer)
        {
            if (value.Kind != DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }

            AppendSpanFormattable(value, sb, buffer, "o");
        }

        private static void WriteValue(DateTimeOffset value, StringBuilder sb, char[] buffer)
        {
            AppendSpanFormattable(value, sb, buffer, "o");
            //if (value.Offset == TimeSpan.Zero)
            //{
            //    AppendSpanFormattable(value, sb, buffer, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK");
            //}
            //else
            //{
            //    AppendSpanFormattable(value, sb, buffer, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK");
            //}
        }

        private static void WriteValue(Guid value, StringBuilder sb, char[] buffer) => AppendSpanFormattable(value, sb, buffer);

        private static void WriteValue(char[] chars, int length, XmlWriter writer)
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

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.CoreLib/src/System/Text/StringBuilder.cs#L1176
        private static void AppendSpanFormattable<T>(T value, StringBuilder sb, char[] buffer, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            if (value.TryFormat(buffer, out int charsWritten, format: format, provider: InvariantCulture))
            {
                sb.Append(buffer, 0, charsWritten);
            }
            else
            {
                throw new Exception("Should not happen.");
            }
        }

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.Xml/src/System/Xml/XmlConvert.cs#L1459
        private static bool IsNegativeZero(double value)
        {
            // Simple equals function will report that -0 is equal to +0, so compare bits instead
            return
                value == 0 &&
                BitConverter.DoubleToInt64Bits(value) == BitConverter.DoubleToInt64Bits(-0e0);
        }

        public static string GetXml(IEnumerable<byte> values)
        {
            return GetXmlByte(values);
        }

        public static string GetXml(IEnumerable<short> values)
        {
            return GetXmlInt16(values);
        }

        public static string GetXml(IEnumerable<int> values)
        {
            return GetXmlInt32(values);
        }

        public static string GetXml(IEnumerable<long> values)
        {
            return GetXmlInt64(values);
        }

        public static string GetXml(IEnumerable<decimal> values)
        {
            return GetXmlDecimal(values);
        }

        public static string GetXml(IEnumerable<float> values)
        {
            return GetXmlSingle(values);
        }

        public static string GetXml(IEnumerable<double> values)
        {
            return GetXmlDouble(values);
        }

        public static string GetXml(IEnumerable<DateTime> values)
        {
            return GetXmlDateTime(values);
        }

        public static string GetXml(IEnumerable<DateTimeOffset> values)
        {
            return GetXmlDateTimeOffset(values);
        }

        public static string GetXml(IEnumerable<Guid> values)
        {
            return GetXmlGuid(values);
        }

        public static string GetXml(IEnumerable<string> values)
        {
            var sb = StringBuilderPool.Get();

            try
            {
                EnsureCapacity(sb, values, 10);

                var settings = new XmlWriterSettings
                {
                    CheckCharacters = false,
                    ConformanceLevel = ConformanceLevel.Fragment
                };

                using var writer = XmlWriter.Create(sb, settings);

                writer.WriteStartElement("R");

                foreach (var value in values)
                {
                    if (value is null)
                    {
                        continue;
                    }

                    var buffer = BufferPool.Rent(value.Length);

                    try
                    {
                        value.CopyTo(0, buffer, 0, value.Length);

                        writer.WriteStartElement("V");
                        WriteValue(buffer, value.Length, writer);
                        writer.WriteEndElement();
                    }
                    finally
                    {
                        BufferPool.Return(buffer);
                    }
                }

                writer.WriteEndElement();

                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Return(sb);
            }
        }

        public static string GetXml(IEnumerable<char> values)
        {
            var sb = StringBuilderPool.Get();
            var buffer = BufferPool.Rent(1);

            try
            {
                EnsureCapacity(sb, values, 1);

                var settings = new XmlWriterSettings
                {
                    CheckCharacters = false,
                    ConformanceLevel = ConformanceLevel.Fragment
                };

                using var writer = XmlWriter.Create(sb, settings);

                writer.WriteStartElement("R");

                foreach (var value in values)
                {
                    buffer[0] = value;

                    writer.WriteStartElement("V");
                    WriteValue(buffer, 1, writer);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                return sb.ToString();
            }
            finally
            {
                BufferPool.Return(buffer);
                StringBuilderPool.Return(sb);
            }
        }
    }
}
