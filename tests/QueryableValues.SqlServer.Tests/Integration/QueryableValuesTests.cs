#if TESTS && TEST_ALL
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    [Collection("DbContext")]
    public class QueryableValuesTests
    {
        private readonly MyDbContext _db;

        public QueryableValuesTests(DbContextFixture contextFixture)
        {
            _db = contextFixture.Db;
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

        private static IEnumerable<decimal> GetSequenceOfDecimals(int numberOfDecimals)
        {
            var fractions = new List<decimal>() { 0 };

            var v = 1M;

            for (int i = 0; i < numberOfDecimals; i++)
            {
                fractions.Add(1 / (v *= 10));
            }

            yield return truncate(-123456.123456M);
            yield return truncate(123456.123456M);
            yield return truncate(-999_999_999_999.999999M);
            yield return truncate(999_999_999_999.999999M);

            for (decimal i = 0; i <= 1_000_000; i *= 10)
            {
                foreach (var f in fractions)
                {
                    var n = i + f;

                    yield return n; ;

                    if (n > 0)
                    {
                        yield return -n;
                    }
                }

                if (i == 0)
                {
                    i = 1;
                }
            }

            decimal truncate(decimal value)
            {
                var step = (decimal)Math.Pow(10, numberOfDecimals);
                return Math.Truncate(step * value) / step;
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfDecimal()
        {
            {
                var expected = GetSequenceOfDecimals(0).ToList();
                var actual = await _db.AsQueryableValues(expected, 0).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = GetSequenceOfDecimals(2).ToList();
                var actual = await _db.AsQueryableValues(expected, 2).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = GetSequenceOfDecimals(6).ToList();
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
        public async Task MustMatchSequenceOfDouble()
        {
            {
                var expected = GetSequenceOfDecimals(0).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = GetSequenceOfDecimals(2).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var expected = GetSequenceOfDecimals(6).Select(i => (double)i).ToList();
                var actual = await _db.AsQueryableValues(expected).ToListAsync();
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfString()
        {
            var values = new[] { "Test 1", "Test <2>", "Test &3", "😀", "ᴭ", "" };

            {
                var expected = new[] { "Test 1", "Test <2>", "Test &3", "??", "?", "" };
                var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();
                Assert.Equal(expected, actual);
            }

            {
                var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();
                Assert.Equal(values, actual);
            }

            {
                var actual = await _db.AsQueryableValues(Array.Empty<string>()).ToListAsync();
                Assert.Empty(actual);
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
        public async Task QueryEntityUnicodeString()
        {
            var values = new[] {
                "👋",
                "你好！"
            };

            var expected = new[] { 1, 2 };

            var actual = await (
                from i in _db.TestData
                join v in _db.AsQueryableValues(values, isUnicode: true) on i.UnicodeStringValue equals v
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
    }
}
#endif