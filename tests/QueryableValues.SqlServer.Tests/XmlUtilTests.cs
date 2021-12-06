#if TESTS
using System;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    public class XmlUtilTests
    {
        [Fact]
        public void IsValidXmlForInt32()
        {
            var values = new[] { int.MinValue, int.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{int.MinValue}</V><V>{int.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForInt64()
        {
            var values = new[] { long.MinValue, long.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{long.MinValue}</V><V>{long.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDecimal()
        {
            var values = new[] { decimal.MinValue, decimal.MaxValue, decimal.Zero, decimal.One, decimal.MinusOne };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{decimal.MinValue}</V><V>{decimal.MaxValue}</V><V>{decimal.Zero}</V><V>{decimal.One}</V><V>{decimal.MinusOne}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForDouble()
        {
            var values = new[] { double.MinValue, double.MaxValue, 0, 1, -1 };
            var xml = XmlUtil.GetXml(values);
            var expected = $"<R><V>{double.MinValue}</V><V>{double.MaxValue}</V><V>{0}</V><V>{1}</V><V>{-1}</V></R>";
            Assert.Equal(expected, xml);
        }

        [Fact]
        public void IsValidXmlForString()
        {
            var values = new[] { "Test 1", "Test <2>", "Test &3", "😀", "ᴭ", "" };
            var xml = XmlUtil.GetXml(values);
            var expected = "<R><V>Test 1</V><V>Test &lt;2&gt;</V><V>Test &amp;3</V><V>😀</V><V>ᴭ</V><V></V></R>";
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
    }
}
#endif