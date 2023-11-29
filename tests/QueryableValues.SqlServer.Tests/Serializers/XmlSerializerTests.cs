#if TESTS
using BlazarTech.QueryableValues.Serializers;
using System;
using System.Linq;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Serializers
{
    public class XmlSerializerTests
    {
        private readonly IXmlSerializer _serializer;

        public XmlSerializerTests()
        {
            _serializer = new XmlSerializer();
        }

        [Fact]
        public void IsValidXmlForComplexType()
        {
            var now = DateTime.Now;

            now = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), now.Kind)
                .AddMilliseconds(123);

            var utcNow = DateTime.UtcNow;

            utcNow = new DateTime(utcNow.Ticks - (utcNow.Ticks % TimeSpan.TicksPerSecond), utcNow.Kind)
                .AddMilliseconds(123);

            var nowString = DateTime.SpecifyKind(now, DateTimeKind.Unspecified).ToString("o").Substring(0, 23);
            var utcNowString = DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified).ToString("o").Substring(0, 23);

            var testType = new
            {
                BooleanTrueValue = true,
                BooleanFalseValue = false,

                ByteMinValue = byte.MinValue,
                ByteMaxValue = byte.MaxValue,

                Int16MinValue = short.MinValue,
                Int16MaxValue = short.MaxValue,
                Int16Zero = (short)0,
                Int16One = (short)1,
                Int16MinusOne = (short)-1,

                Int32MinValue = int.MinValue,
                Int32MaxValue = int.MaxValue,
                Int32Zero = 0,
                Int32One = 1,
                Int32MinusOne = -1,

                Int64MinValue = long.MinValue,
                Int64MaxValue = long.MaxValue,
                Int64Zero = 0L,
                Int64One = 1L,
                Int64MinusOne = -1L,

                DecimalMinValue = decimal.MinValue,
                DecimalMaxValue = decimal.MaxValue,
                DecimalZero = decimal.Zero,
                DecimalOne = decimal.One,
                DecimalMinusOne = decimal.MinusOne,
                DecimalValue = 123.456789M,

                SingleMinValue = float.MinValue,
                SingleMaxValue = float.MaxValue,
                SingleZero = 0F,
                SingleOne = 1F,
                SingleMinusOne = -1F,
                SingleValue = 123.456789F,

                DoubleMinValue = double.MinValue,
                DoubleMaxValue = double.MaxValue,
                DoubleZero = 0D,
                DoubleOne = 1D,
                DoubleMinusOne = -1D,
                DoubleValue = 123.456789D,

                DateTimeMinValue = DateTime.MinValue,
                DateTimeMaxValue = DateTime.MaxValue,
                DateTimeMinValueLocal = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local),
                DateTimeMaxValueLocal = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local),
                DateTimeMinValueUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                DateTimeMaxValueUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
                DateTimeNow = now,
                DateTimeUtcNow = utcNow,

                DateTimeOffsetMinValue = DateTimeOffset.MinValue,
                DateTimeOffsetMaxValue = DateTimeOffset.MaxValue,
                DateTimeOffset1 = new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(0)),
                DateTimeOffset2 = new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(5.5)),

                Guid1 = Guid.Empty,
                Guid2 = Guid.Parse("b8f66b9f-a9ee-447a-bd10-6b6adb9bcfaf"),

                Char1 = ' ',
                Char2 = 'a',
                Char3 = 'A',
                Char4 = '1',
                Char5 = '0',
                Char6 = '\n',
                Char7 = '\0',
                Char8 = '☃',

                String1 = " Hi!\n😀\"",
                String2 = "Test 1",
                String3 = "Test <2>",
                String4 = "Test &3",
                String5 = (string?)null,
                String6 = "😀",
                String7 = "ᴭ",
                String8 = "",
                String9 = " ",
            };

            var values = new[] { testType };
            var mappings = EntityPropertyMapping.GetMappings(testType.GetType(), typeof(ComplexQueryableValuesEntity));
            var actual = _serializer.Serialize(values, mappings);

            var expectedByte = $@"Y=""{byte.MinValue}"" Y1=""{byte.MaxValue}""";
            var expectedInt16 = $@"H=""{short.MinValue}"" H1=""{short.MaxValue}"" H2=""0"" H3=""1"" H4=""-1""";
            var expectedInt32 = $@"I=""{int.MinValue}"" I1=""{int.MaxValue}"" I2=""0"" I3=""1"" I4=""-1""";
            var expectedInt64 = $@"L=""{long.MinValue}"" L1=""{long.MaxValue}"" L2=""0"" L3=""1"" L4=""-1""";
            var expectedDecimal = $@"M=""{decimal.MinValue}"" M1=""{decimal.MaxValue}"" M2=""{decimal.Zero}"" M3=""{decimal.One}"" M4=""{decimal.MinusOne}"" M5=""123.456789""";
            var expectedSingle = $@"F=""{float.MinValue}"" F1=""{float.MaxValue}"" F2=""0"" F3=""1"" F4=""-1"" F5=""123.45679""";
            var expectedDouble = $@"D=""{double.MinValue}"" D1=""{double.MaxValue}"" D2=""0"" D3=""1"" D4=""-1"" D5=""123.456789""";
            var expectedDateTime = $@"A=""0001-01-01T00:00:00"" A1=""9999-12-31T23:59:59.9999999"" A2=""0001-01-01T00:00:00"" A3=""9999-12-31T23:59:59.9999999"" A4=""0001-01-01T00:00:00"" A5=""9999-12-31T23:59:59.9999999"" A6=""{nowString}"" A7=""{utcNowString}""";
            var expectedDateTimeOffset = $@"E=""0001-01-01T00:00:00Z"" E1=""9999-12-31T23:59:59.9999999Z"" E2=""2021-01-01T01:02:03.004Z"" E3=""2021-01-01T01:02:03.004+05:30""";
            var expectedGuid = $@"G=""00000000-0000-0000-0000-000000000000"" G1=""b8f66b9f-a9ee-447a-bd10-6b6adb9bcfaf""";
            var expectedChar = $@"C=""&#x20;"" C1=""a"" C2=""A"" C3=""1"" C4=""0"" C5=""&#xA;"" C6=""?"" C7=""☃""";
            var expectedString = $@"S=""&#x20;Hi!&#xA;😀&quot;"" S1=""Test&#x20;1"" S2=""Test&#x20;&lt;2&gt;"" S3=""Test&#x20;&amp;3"" S5=""😀"" S6=""ᴭ"" S7="""" S8=""&#x20;""";

            var expected = $@"<R><V X=""0"" B=""1"" B1=""0"" {expectedByte} {expectedInt16} {expectedInt32} {expectedInt64} {expectedDecimal} {expectedSingle} {expectedDouble} {expectedDateTime} {expectedDateTimeOffset} {expectedGuid} {expectedChar} {expectedString} /></R>";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsValidEmptyXmlForComplexType()
        {
            var testType = new
            {
                A = 1
            };

            var values = new[] { testType };
            var mappings = EntityPropertyMapping.GetMappings(testType.GetType(), typeof(ComplexQueryableValuesEntity));
            var xml = _serializer.Serialize(values.Take(0), mappings);
            Assert.Equal("<R />", xml);
        }
    }
}
#endif