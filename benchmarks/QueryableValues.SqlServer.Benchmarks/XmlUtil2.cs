using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private static string GetXml<T>(
            IEnumerable<T> values,
            Action<T, StringBuilder, char[]> writeValue,
            Func<T, bool>? mustSkipValue = null,
            int valueMinLength = 0,
            bool useBuffer = false
            )
        {
            var capacity = 0;

            if (valueMinLength > 0 && values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = ((valueMinLength + 7) * count) + 7;
            }

            var sb = StringBuilderPool.Get();
            var buffer = useBuffer ? BufferPool.Rent(ValueBufferSize) : Array.Empty<char>();
            //var buffer = useBuffer ? new char[128] : Array.Empty<char>();

            try
            {
                if (capacity > 0)
                {
                    sb.EnsureCapacity(capacity);
                }

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
    }
}
