#if TESTS && TEST_ALL
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    public abstract class SimpleTypeTests
    {
        protected readonly IMyDbContext _db;

        public SimpleTypeTests(DbContextFixture contextFixture)
        {
            _db = contextFixture.Db;
        }

        [Fact]
        public async Task MustValidateEnumerationCount()
        {
            var enumerationCount = 0;

            IEnumerable<int> enumerableData()
            {
                enumerationCount++;
                yield return 1;
            }

            var query = _db.AsQueryableValues(enumerableData());

            Assert.Equal(0, enumerationCount);

            var expectedEnumerationCount = 0;

            _ = await query.FirstAsync();
            _ = await query.FirstAsync();

#if EFCORE3
            // Under EF Core 3, enumerableData gets enumerated only once...
            expectedEnumerationCount = 1;
#else
            expectedEnumerationCount = 2;
#endif

            Assert.Equal(expectedEnumerationCount, enumerationCount);
        }

        [Fact]
        public async Task MustSeeAllItemsFromCollection()
        {
            var expected = new List<int>();

            var query = _db.AsQueryableValues(expected);

            var actual = await query.ToListAsync();
            Assert.Equal(expected, actual);

            for (int i = 0; i <= 3; i++)
            {
                expected.Add(i);

                actual = await query.ToListAsync();
#if EFCORE3
                // EF Core 3 do NOT support this.
                // Keep to check if this behavior changes.
                Assert.NotEqual(expected, actual);
#else
                Assert.Equal(expected, actual);
#endif
            }
        }

        [Fact]
        public async Task MustSeeAllItemsFromNonCollection()
        {
            var count = 0;

            IEnumerable<int> getIds()
            {
                for (int i = 0; i < count; i++)
                {
                    yield return i;
                }
            }

            var expected = getIds();
            var query = _db.AsQueryableValues(expected);

            var actual = await query.ToListAsync();
            Assert.Equal(expected, actual);

            for (int i = 0; i < 3; i++)
            {
                count++;
                actual = await query.ToListAsync();
#if EFCORE3
                // EF Core 3 do NOT support this.
                // Keep to check if this behavior changes.
                Assert.NotEqual(expected, actual);
#else
                Assert.Equal(expected, actual);
#endif
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfByte()
        {
            var expected = Enumerable.Range(0, 256).Select(i => (byte)i);
            var actual = await _db.AsQueryableValues(expected).ToListAsync();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfInt16()
        {
            var expected = Enumerable.Range(0, 10).Select(i => (short)i);
            var actual = await _db.AsQueryableValues(expected).ToListAsync();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfInt32()
        {
            var expected = Enumerable.Range(0, 10);
            var actual = await _db.AsQueryableValues(expected).ToListAsync();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfInt64()
        {
            var expected = Enumerable.Range(0, 10).Select(i => (long)i);
            var actual = await _db.AsQueryableValues(expected).ToListAsync();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfDecimal()
        {
            {
                var expected = TestUtil.GetSequenceOfDecimals(0).ToList();
                var actual = await _db.AsQueryableValues(expected, 0).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = TestUtil.GetSequenceOfDecimals(2).ToList();
                var actual = await _db.AsQueryableValues(expected, 2).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = TestUtil.GetSequenceOfDecimals(6).ToList();
                var actual = await _db.AsQueryableValues(expected, 6).ToListAsync();
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task MustFailSequenceOfDecimalInvalidNumberOfDecimals()
        {
            await Assert.ThrowsAsync<ArgumentException>("numberOfDecimals", async () =>
            {
                _ = await _db.AsQueryableValues(Array.Empty<decimal>(), -1).ToListAsync();
            });

            await Assert.ThrowsAsync<ArgumentException>("numberOfDecimals", async () =>
            {
                _ = await _db.AsQueryableValues(Array.Empty<decimal>(), 39).ToListAsync();
            });
        }

        [Fact]
        public async Task MustMatchSequenceOfSingle()
        {
            var expected = new float[] { -3.402823E+38F, 3.402823E+38F, 0, 1, 9999999, 1234567, 123456.7F, 12345.67F, 1.234567F };
            var actual = await _db.AsQueryableValues(expected).ToListAsync();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfDouble()
        {
            {
                var expected = TestUtil.GetSequenceOfDecimals(0).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = TestUtil.GetSequenceOfDecimals(2).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = TestUtil.GetSequenceOfDecimals(6).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfChar()
        {
            var values = new[] { 'A', 'a', 'ᴭ', ' ', '\n', '\0', '\u0001' };

            if (_db.Options.WithSerializationOptions == SerializationOptions.UseXml)
            {
                {
                    var expected = new[] { 'A', 'a', '?', ' ', '\n', '?', '?' };
                    var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();
                    Assert.Equal(expected, actual);
                }

                {
                    var expected = new[] { 'A', 'a', 'ᴭ', ' ', '\n', '?', '?' };
                    var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();
                    Assert.Equal(expected, actual);
                }
            }
            else if (_db.Options.WithSerializationOptions == SerializationOptions.UseJson)
            {
                {
                    var expected = new[] { 'A', 'a', '?', ' ', '\n', '\0', '\u0001' };
                    var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();
                    Assert.Equal(expected, actual);
                }

                {
                    var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();
                    Assert.Equal(values, actual);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            {
                var many = Enumerable.Range(0, 1000).Select(i => (char)i);
                var actual = await _db.AsQueryableValues(many, isUnicode: true).ToListAsync();
                Assert.Equal(1000, actual.Count);
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfString()
        {
            var values = new[] { "\0 ", "\u0001", "Test 1", "Test <2>", "Test &3", "😀", "ᴭ", "", " ", "\n", " \n", " \n ", "\r", "\r ", " Test\t1 ", "\U00010330" };

            if (_db.Options.WithSerializationOptions == SerializationOptions.UseXml)
            {
                {
                    var expected = new string[values.Length];
                    values.CopyTo(expected, 0);
                    expected[0] = "? ";
                    expected[1] = "?";
                    expected[5] = "??";
                    expected[6] = "?";
                    expected[15] = "??";

                    var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();

                    Assert.Equal(expected, actual);
                }

                {
                    var expected = new string[values.Length];
                    values.CopyTo(expected, 0);
                    expected[0] = "? ";
                    expected[1] = "?";

                    var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();

                    Assert.Equal(expected, actual);
                }
            }
            else if (_db.Options.WithSerializationOptions == SerializationOptions.UseJson)
            {
                {
                    var expected = new string[values.Length];
                    values.CopyTo(expected, 0);
                    expected[5] = "??";
                    expected[6] = "?";
                    expected[15] = "??";

                    var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();

                    Assert.Equal(expected, actual);
                }

                {
                    var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();
                    Assert.Equal(values, actual);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfDateTime()
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

            var actual = await _db.AsQueryableValues(values).ToListAsync();
            Assert.Equal(values, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfDateTimeOffset()
        {
            var values = new[] {
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(0)),
                new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(5.5))
            };

            var actual = await _db.AsQueryableValues(values).ToListAsync();

            Assert.Equal(values, actual);
        }

        [Fact]
        public async Task MustMatchSequenceOfGuid()
        {
            var expected = new[] {
                Guid.Empty,
                Guid.Parse("5a9354e2-ba25-4acc-a365-6a2594980879"),
                Guid.Empty,
                Guid.Parse("9816e5dc-56c1-44c5-88b8-3557c4fc55b7")
            };

            var actual = await _db.AsQueryableValues(expected).ToListAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityByte()
        {
            var values = new[] {
                byte.MaxValue
            };

            var expected = new[] { 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.ByteValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityInt16()
        {
            var values = new short[] {
                0,
                short.MaxValue
            };

            var expected = new[] { 2, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.Int16Value equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityInt32()
        {
            var values = new[] {
                0,
                int.MaxValue,
                int.MaxValue
            };

            var expected = new[] { 2, 3, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.Int32Value equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityInt64()
        {
            var values = new[] {
                long.MinValue,
                long.MaxValue
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.Int64Value equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityDecimal()
        {
            var values = new[] {
                -1234567.890123M,
                1234567.890123M
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values, numberOfDecimals: 6) on i.DecimalValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntitySingle()
        {
            var values = new[] {
                12345.67F,
                3.402823E+38F
            };

            var expected = new[] { 2, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.SingleValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityDouble()
        {
            var values = new[] {
                -1234567.890123D,
                1234567.890123D
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.DoubleValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityChar()
        {
            var values = new[] {
                'A',
                'c'
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.CharValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityCharUnicode()
        {
            var values = new[] {
                '☢',
                '你'
            };

            var expected = new[] { 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values, isUnicode: true) on i.CharUnicodeValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityString()
        {
            var values = new[] {
                "Hi!",
                "Hola!"
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.StringValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityStringUnicode()
        {
            var values = new[] {
                "👋",
                "你好！"
            };

            var expected = new[] { 1, 2 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values, isUnicode: true) on i.StringUnicodeValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task QueryEntityDateTime()
        {
            var dateTimeOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.FromHours(5));

            var values = new[] {
                DateTime.MinValue,
                dateTimeOffset.DateTime
            };

            var expected = new[] { 1, 2 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.DateTimeValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }


        [Fact]
        public async Task QueryEntityDateTimeOffset()
        {
            var dateTimeOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.FromHours(5));

            var values = new[] {
                DateTimeOffset.MinValue,
                dateTimeOffset
            };

            var expected = new[] { 1, 2 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.DateTimeOffsetValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }


        [Fact]
        public async Task QueryEntityGuid()
        {
            var values = new[] {
                Guid.Empty,
                Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6")
            };

            var expected = new[] { 1, 3 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values) on i.GuidValue equals v
                orderby i.Id
                select i.Id
                )
                .ToArrayAsync();

            Assert.Equal(expected, actual);
        }


        [Fact]
        public async Task MustBeEmpty()
        {
            var testCounter = 0;

            await AssertEmpty<byte>();
            await AssertEmpty<short>();
            await AssertEmpty<int>();
            await AssertEmpty<long>();
            await AssertEmpty<decimal>();
            await AssertEmpty<float>();
            await AssertEmpty<double>();
            await AssertEmpty<DateTime>();
            await AssertEmpty<DateTimeOffset>();
            await AssertEmpty<Guid>();
            await AssertEmpty<char>();
            await AssertEmpty<string>();

            // Coverage check.
            var expectedTestCount = EntityPropertyMapping.SimpleTypes.Count - 1;
            Assert.Equal(expectedTestCount, testCounter);

            async Task AssertEmpty<T>()
                where T : notnull
            {
                testCounter++;
                var actual = await _db.AsQueryableValues<T>(Array.Empty<T>()).ToListAsync();
                Assert.Empty(actual);
            }
        }

        [Fact]
        public async Task MustMatchCount()
        {
            const int expectedItemCount = 2500;

            var testCounter = 0;
            var helperBytes = new byte[8];

            await AssertCount<byte>(i => (byte)i);
            await AssertCount<short>(i => (short)i);
            await AssertCount<int>(i => i);
            await AssertCount<long>(i => (long)i);
            await AssertCount<decimal>(i => (decimal)i);
            await AssertCount<float>(i => (float)i);
            await AssertCount<double>(i => (double)i);
            await AssertCount<DateTime>(i => DateTime.MinValue.AddDays(i));
            await AssertCount<DateTimeOffset>(i => DateTimeOffset.MinValue.AddDays(i));
            await AssertCount<Guid>(i => new Guid(i, 0, 0, helperBytes));
            await AssertCount<char>(i => 'A');
            await AssertCount<string>(i => $"Test {i}");

            // Coverage check.
            var expectedTestCount = EntityPropertyMapping.SimpleTypes.Count - 1;
            Assert.Equal(expectedTestCount, testCounter);

            async Task AssertCount<T>(Func<int, T> getValue)
                where T : notnull
            {
                testCounter++;
                var values = Enumerable.Range(0, expectedItemCount).Select(i => getValue(i));
                var actualItemCount = await _db.AsQueryableValues<T>(values).CountAsync();
                Assert.Equal(expectedItemCount, actualItemCount);
            }
        }
    }

    [Collection("DbContext")]
    public class JsonSimpleTypeTests : SimpleTypeTests
    {
        public JsonSimpleTypeTests(DbContextFixture contextFixture) : base(contextFixture)
        {
            _db.Options.Serialization(SerializationOptions.UseJson);
        }
    }

    [Collection("DbContext")]
    public class XmlSimpleTypeTests : SimpleTypeTests
    {
        public XmlSimpleTypeTests(DbContextFixture contextFixture) : base(contextFixture)
        {
            _db.Options.Serialization(SerializationOptions.UseXml);
        }
    }
}
#endif