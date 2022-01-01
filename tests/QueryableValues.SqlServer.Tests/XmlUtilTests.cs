#if TESTS
using System;
using System.Linq;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    public class XmlUtilTests
    {
        [Fact]
        public void IsValidXmlForByte()
        {
            var values = new byte[] { byte.MinValue, byte.MaxValue };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{byte.MinValue}</V><V>{byte.MaxValue}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForInt16()
        {
            var values = new short[] { short.MinValue, short.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{short.MinValue}</V><V>{short.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForInt32()
        {
            var values = new int[] { int.MinValue, int.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{int.MinValue}</V><V>{int.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForInt64()
        {
            var values = new long[] { long.MinValue, long.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{long.MinValue}</V><V>{long.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDecimal()
        {
            var values = new decimal[] { decimal.MinValue, decimal.MaxValue, decimal.Zero, decimal.One, decimal.MinusOne };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{decimal.MinValue}</V><V>{decimal.MaxValue}</V><V>{decimal.Zero}</V><V>{decimal.One}</V><V>{decimal.MinusOne}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForSingle()
        {
            var values = new float[] { float.MinValue, float.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{float.MinValue}</V><V>{float.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDouble()
        {
            var values = new double[] { double.MinValue, double.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{double.MinValue}</V><V>{double.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDateTime()
        {
            var now = DateTime.Now;

            now = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), now.Kind)
                .AddMilliseconds(123);

            var utcNow = DateTime.UtcNow;

            utcNow = new DateTime(utcNow.Ticks - (utcNow.Ticks % TimeSpan.TicksPerSecond), utcNow.Kind)
                .AddMilliseconds(123);

            var values = new[] {
                DateTime.MinValue,
                DateTime.MaxValue,
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local),
                DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local),
                DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
                now,
                utcNow
            };

            var xml = XmlUtil.GetXml(values);
            var nowString = DateTime.SpecifyKind(now, DateTimeKind.Unspecified).ToString("o").Substring(0, 23);
            var utcNowString = DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified).ToString("o").Substring(0, 23);
            var expected = $"<R><V>0001-01-01T00:00:00</V><V>9999-12-31T23:59:59.9999999</V><V>0001-01-01T00:00:00</V><V>9999-12-31T23:59:59.9999999</V><V>0001-01-01T00:00:00</V><V>9999-12-31T23:59:59.9999999</V><V>{nowString}</V><V>{utcNowString}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDateTimeOffset()
        {
            var values = new[] {
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(0)),
                new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(5.5))
            };

            var xml = XmlUtil.GetXml(values);
            var expected = "<R><V>0001-01-01T00:00:00Z</V><V>9999-12-31T23:59:59.9999999Z</V><V>2021-01-01T01:02:03.004Z</V><V>2021-01-01T01:02:03.004+05:30</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForGuid()
        {
            var values = new[] {
                Guid.Empty,
                Guid.Parse("b8f66b9f-a9ee-447a-bd10-6b6adb9bcfaf")
            };
            var xml = XmlUtil.GetXml(values);
            var expected = "<R><V>00000000-0000-0000-0000-000000000000</V><V>b8f66b9f-a9ee-447a-bd10-6b6adb9bcfaf</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForChar()
        {
            var values = new[] { ' ', 'a', 'A', '1', '0', '\n', '\0', '☃' };
            var xml = XmlUtil.GetXml(values);
            var expected = "<R><V>&#x20;</V><V>a</V><V>A</V><V>1</V><V>0</V><V>&#xA;</V><V>?</V><V>☃</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForString()
        {
            var values = new[] { "Test 1", "Test <2>", "Test &3", "😀", "ᴭ", "", " " };
            var xml = XmlUtil.GetXml(values);
            var expected = "<R><V>Test&#x20;1</V><V>Test&#x20;&lt;2&gt;</V><V>Test&#x20;&amp;3</V><V>😀</V><V>ᴭ</V><V /><V>&#x20;</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForComplexType()
        {
            var testType = new
            {
                BooleanTrueValue = true,
                BooleanFalseValue = false,
                ByteValue = byte.MaxValue,
                Int16Value = short.MaxValue,
                Int32Value = int.MaxValue,
                Int64Value = long.MaxValue,
                DecimalValue = decimal.MaxValue,
                SingleValue = float.MaxValue,
                DoubleValue = double.MaxValue,
                DateTimeValue = DateTime.MaxValue,
                DateTimeOffsetValue = DateTimeOffset.MaxValue,
                GuidValue = Guid.Empty,
                CharValue = '☢',
                StringValue = " Hi!\n😀\"",
                StringNullValue = (string?)null,
                StringEmptyValue = ""
            };

            var values = new[] { testType };
            var mappings = EntityPropertyMapping.GetMappings(testType.GetType());
            var xml = XmlUtil.GetXml(values, mappings);
            var expected = "<R><V Bool0=\"1\" Bool1=\"0\" Byte0=\"255\" Short0=\"32767\" Int0=\"2147483647\" Long0=\"9223372036854775807\" Decimal0=\"79228162514264337593543950335\" Float0=\"3.4028235E+38\" Double0=\"1.7976931348623157E+308\" DateTime0=\"9999-12-31T23:59:59.9999999\" DateTimeOffset0=\"9999-12-31T23:59:59.9999999Z\" Guid0=\"00000000-0000-0000-0000-000000000000\" Char0=\"☢\" String0=\"&#x20;Hi!&#xA;😀&quot;\" String2=\"\" /></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidEmptyXmlForSimpleType()
        {
            var testCounter = 0;

            AssertXml(XmlUtil.GetXml(Array.Empty<byte>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<short>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<int>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<long>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<decimal>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<float>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<double>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<DateTime>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<DateTimeOffset>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<Guid>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<char>()));
            AssertXml(XmlUtil.GetXml(Array.Empty<string>()));

            // Coverage check.
            var expectedTestCount = EntityPropertyMapping.SimpleTypes.Count - 1;
            Assert.Equal(expectedTestCount, testCounter);

            void AssertXml(string xml)
            {
                testCounter++;
                Assert.Equal("<R />", xml);
            }
        }

        [Fact]
        public void IsValidEmptyXmlForComplexType()
        {
            var testType = new
            {
                A = 1
            };

            var values = new[] { testType };
            var mappings = EntityPropertyMapping.GetMappings(testType.GetType());
            var xml = XmlUtil.GetXml(values.Take(0), mappings);
            Assert.Equal("<R />", xml);
        }
    }
}
#endif