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
        private const int ValueBufferLength = 128;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        private static readonly DefaultObjectPool<StringBuilder> StringBuilderPool = new(new StringBuilderPooledObjectPolicy
        {
            InitialCapacity = 100,
            MaximumRetainedCapacity = 512_000
        });

        private static readonly ArrayPool<char> BufferPool = ArrayPool<char>.Shared;

        private static readonly Func<IEnumerable<byte>, string> GetXmlByte = (IEnumerable<byte> values) => GetXml(values, WriteValue, 3);
        private static readonly Func<IEnumerable<short>, string> GetXmlInt16 = (IEnumerable<short> values) => GetXml(values, WriteValue, 3);
        private static readonly Func<IEnumerable<int>, string> GetXmlInt32 = (IEnumerable<int> values) => GetXml(values, WriteValue, 5);
        private static readonly Func<IEnumerable<long>, string> GetXmlInt64 = (IEnumerable<long> values) => GetXml(values, WriteValue, 10);
        private static readonly Func<IEnumerable<decimal>, string> GetXmlDecimal = (IEnumerable<decimal> values) => GetXml(values, WriteValue, 10, true);
        private static readonly Func<IEnumerable<float>, string> GetXmlSingle = (IEnumerable<float> values) => GetXml(values, WriteValue, 10, true);
        private static readonly Func<IEnumerable<double>, string> GetXmlDouble = (IEnumerable<double> values) => GetXml(values, WriteValue, 10, true);
        private static readonly Func<IEnumerable<DateTime>, string> GetXmlDateTime = (IEnumerable<DateTime> values) => GetXml(values, WriteValue, 27, true);
        private static readonly Func<IEnumerable<DateTimeOffset>, string> GetXmlDateTimeOffset = (IEnumerable<DateTimeOffset> values) => GetXml(values, WriteValue, 33, true);
        private static readonly Func<IEnumerable<Guid>, string> GetXmlGuid = (IEnumerable<Guid> values) => GetXml(values, WriteValue, 36, true);

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
            Action<WriterHelper, T> writeValue,
            int valueMinLength = 0,
            bool useBuffer = false
            )
        {
            using var writer = new WriterHelper(ValueBufferLength);

            EnsureCapacity(writer.Sb, values, valueMinLength);

            writer.Sb.Append("<R>");

            foreach (var value in values)
            {
                writer.Sb.Append("<V>");
                writeValue(writer, value);
                writer.Sb.Append("</V>");
            }

            writer.Sb.Append("</R>");

            return writer.Sb.ToString();
        }

        private static void WriteValue(WriterHelper writer, bool value) => writer.Sb.Append(value ? '1' : '0');

        private static void WriteValue(WriterHelper writer, byte value) => writer.Sb.Append(value);

        private static void WriteValue(WriterHelper writer, short value) => writer.Sb.Append(value);

        private static void WriteValue(WriterHelper writer, int value) => writer.Sb.Append(value);

        private static void WriteValue(WriterHelper writer, long value) => writer.Sb.Append(value);

        private static void WriteValue(WriterHelper writer, decimal value) => AppendSpanFormattable(writer, value);

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.Xml/src/System/Xml/XmlConvert.cs#L726
        private static void WriteValue(WriterHelper writer, float value)
        {
            if (float.IsNegativeInfinity(value))
            {
                writer.Sb.Append("-INF");
            }
            else if (float.IsPositiveInfinity(value))
            {
                writer.Sb.Append("INF");
            }
            else if (IsNegativeZero(value))
            {
                writer.Sb.Append("-0");
            }
            else
            {
                AppendSpanFormattable(writer, value, "R");
            }
        }

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.Xml/src/System/Xml/XmlConvert.cs#L737
        private static void WriteValue(WriterHelper writer, double value)
        {
            if (double.IsNegativeInfinity(value))
            {
                writer.Sb.Append("-INF");
            }
            else if (double.IsPositiveInfinity(value))
            {
                writer.Sb.Append("INF");
            }
            else if (IsNegativeZero(value))
            {
                writer.Sb.Append("-0");
            }
            else
            {
                AppendSpanFormattable(writer, value, "R");
            }
        }

        private static void WriteValue(WriterHelper writer, DateTime value)
        {
            if (value.Kind != DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            }

            AppendSpanFormattable(writer, value, "o");
        }

        private static void WriteValue(WriterHelper writer, DateTimeOffset value)
        {
            AppendSpanFormattable(writer, value, "o");
            //if (value.Offset == TimeSpan.Zero)
            //{
            //    AppendSpanFormattable(value, sb, buffer, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK");
            //}
            //else
            //{
            //    AppendSpanFormattable(value, sb, buffer, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK");
            //}
        }

        private static void WriteValue(WriterHelper writer, Guid value) => AppendSpanFormattable(writer, value);

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

        // https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Private.CoreLib/src/System/Text/StringBuilder.cs#L1176
        private static void AppendSpanFormattable<T>(WriterHelper writer, T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            if (value.TryFormat(writer.Buffer, out int charsWritten, format: format, provider: InvariantCulture))
            {
                writer.Sb.Append(writer.Buffer, 0, charsWritten);
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

        private static XmlWriter CreateXmlWriter(StringBuilder sb)
        {
            var settings = new XmlWriterSettings
            {
                CheckCharacters = false,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            return XmlWriter.Create(sb, settings);
        }

        public static string GetXml(IEnumerable<string> values)
        {
            const int defaultBufferLength = 25;

            var sb = StringBuilderPool.Get();

            try
            {
                EnsureCapacity(sb, values, defaultBufferLength);

                using var writer = CreateXmlWriter(sb);

                writer.WriteStartElement("R");

                var buffer = BufferPool.Rent(defaultBufferLength);

                // buffer.Length may be bigger than defaultBufferLength.
                var lastLength = buffer.Length;

                try
                {
                    foreach (var value in values)
                    {
                        if (value is null)
                        {
                            continue;
                        }

                        if (value.Length > lastLength)
                        {
                            BufferPool.Return(buffer);
                            buffer = BufferPool.Rent(value.Length);
                            lastLength = buffer.Length;
                        }

                        value.CopyTo(0, buffer, 0, value.Length);

                        writer.WriteStartElement("V");
                        WriteValue(writer, buffer, value.Length);
                        writer.WriteEndElement();
                    }
                }
                finally
                {
                    BufferPool.Return(buffer);
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

                using var writer = CreateXmlWriter(sb);

                writer.WriteStartElement("R");

                foreach (var value in values)
                {
                    buffer[0] = value;

                    writer.WriteStartElement("V");
                    WriteValue(writer, buffer, 1);
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
    }
}
