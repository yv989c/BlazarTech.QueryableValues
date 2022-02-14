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
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly DefaultObjectPool<StringBuilder> DefaultObjectPool = new(new StringBuilderPooledObjectPolicy
        {
            InitialCapacity = 100,
            MaximumRetainedCapacity = 100000
        });
        //private static readonly char[] Buffer = new char[100];
        private static readonly ArrayPool<char> BufferPool = ArrayPool<char>.Shared;

        private static readonly Func<IEnumerable<byte>, string> GetXmlByte = (IEnumerable<byte> values) => GetXml(values, WriteValue, null, 3);
        private static readonly Func<IEnumerable<short>, string> GetXmlInt16 = (IEnumerable<short> values) => GetXml(values, WriteValue, null, 3);
        private static readonly Func<IEnumerable<int>, string> GetXmlInt32 = (IEnumerable<int> values) => GetXml(values, WriteValue, null, 5);
        private static readonly Func<IEnumerable<long>, string> GetXmlInt64 = (IEnumerable<long> values) => GetXml(values, WriteValue, null, 10);
        private static readonly Func<IEnumerable<decimal>, string> GetXmlDecimal = (IEnumerable<decimal> values) => GetXml(values, WriteValue, null, 10, 50);

        private static string GetXml<T>(
            IEnumerable<T> values,
            Action<T, StringBuilder, char[]> writeValue,
            Func<T, bool>? mustSkipValue = null,
            int valueMinLength = 0,
            int bufferLength = 0
            )
        {
            var capacity = 100;

            if (valueMinLength > 0 && values.TryGetNonEnumeratedCount(out int count))
            {
                capacity = ((valueMinLength + 7) * count) + 7;
            }

            var sb = DefaultObjectPool.Get();
            var buffer = bufferLength > 0 ? BufferPool.Rent(bufferLength) : Array.Empty<char>();

            try
            {
                sb.EnsureCapacity(capacity);
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
                if (bufferLength > 0)
                {
                    BufferPool.Return(buffer);
                }

                DefaultObjectPool.Return(sb);
            }
        }

        private static void WriteValue(bool value, StringBuilder sb, char[] buffer) => sb.Append(value ? '1' : '0');
        private static void WriteValue(byte value, StringBuilder sb, char[] buffer) => sb.Append(value);
        private static void WriteValue(short value, StringBuilder sb, char[] buffer) => sb.Append(value);
        private static void WriteValue(int value, StringBuilder sb, char[] buffer) => sb.Append(value);
        private static void WriteValue(long value, StringBuilder sb, char[] buffer) => sb.Append(value);
        private static void WriteValue(decimal value, StringBuilder sb, char[] buffer) => AppendSpanFormattable(value, sb, buffer);

        private static void AppendSpanFormattable<T>(T value, StringBuilder sb, char[] buffer) where T : ISpanFormattable
        {
            if (value.TryFormat(buffer, out int charsWritten, format: default, provider: InvariantCulture))
            {
                sb.Append(buffer, 0, charsWritten);
            }
            else
            {
                throw new Exception("Should not happen.");
            }
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
    }
}
