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
    public class ComplexTypeTests
    {
        private readonly MyDbContext _db;

        public class TestType
        {
            public int IntValue { get; set; }
            public int? IntNullableValue { get; set; }
            public long LongValue { get; set; }
            public long? LongNullableValue { get; set; }
            public decimal DecimalValue { get; set; }
            public decimal? DecimalNullableValue { get; set; }
            public double DoubleValue { get; set; }
            public double? DoubleNullableValue { get; set; }
            public DateTime DateTimeValue { get; set; }
            public DateTime? DateTimeNullableValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public DateTimeOffset? DateTimeOffsetNullableValue { get; set; }
            public Guid GuidValue { get; set; }
            public Guid? GuidNullableValue { get; set; }
            public string? StringValue { get; set; }
            public string? StringUnicodeValue { get; set; }
        }

        public struct TestEntityStruct
        {
            public int Id { get; set; }
            public int? OtherId { get; set; }
            public int AnotherId { get; set; }
            public string Greeting { get; set; }
        }

        public ComplexTypeTests(DbContextFixture contextFixture)
        {
            _db = contextFixture.Db;
        }

        // todo: Also add this test to SimpleTypeTests.
        [Fact]
        public async Task MustValidateEnumerationCount()
        {
            var enumerationCount = 0;

            var data = new[]
            {
                new TestType
                {
                    IntValue = int.MinValue,
                    IntNullableValue = int.MaxValue
                }
            };

            IEnumerable<TestType> enumerableData()
            {
                enumerationCount++;

                foreach (var item in data)
                {
                    yield return item;
                }
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
            var expected = new[] { new { Id = 0 } }.Take(0).ToList();

            var query = _db.AsQueryableValues(expected);

            var actual = await query.ToListAsync();
            TestUtil.EqualShape(expected, actual);

            for (int i = 0; i <= 3; i++)
            {
                expected.Add(new { Id = i });

                actual = await query.ToListAsync();
#if EFCORE3
                // EF Core 3 do NOT support this.
                // Keep to check if this behavior changes.
                TestUtil.NotEqualShape(expected, actual);
#else
                TestUtil.EqualShape(expected, actual);
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

            var expected = getIds().Select(i => new { Id = i });
            var query = _db.AsQueryableValues(expected);

            var actual = await query.ToListAsync();
            TestUtil.EqualShape(expected, actual);

            for (int i = 0; i < 3; i++)
            {
                count++;
                actual = await query.ToListAsync();
#if EFCORE3
                // EF Core 3 do NOT support this.
                // Keep to check if this behavior changes.
                TestUtil.NotEqualShape(expected, actual);
#else
                TestUtil.EqualShape(expected, actual);
#endif
            }
        }

        [Fact]
        public async Task MustMatchSequenceOfTestTypeUsesDefaults()
        {
            var expected = new[]
            {
                new TestType
                {
                    IntValue = int.MinValue,
                    IntNullableValue = int.MaxValue,
                    LongValue = long.MinValue,
                    LongNullableValue = long.MaxValue,
                    DecimalValue = 1.1234M,
                    DecimalNullableValue = 999999.1234M,
                    DoubleValue = 1.1234D,
                    DoubleNullableValue = 999999.1234D,
                    DateTimeValue = DateTime.MinValue,
                    DateTimeNullableValue = DateTime.MaxValue,
                    DateTimeOffsetValue = DateTimeOffset.MinValue,
                    DateTimeOffsetNullableValue = DateTimeOffset.MaxValue,
                    GuidValue = Guid.Parse("22ebe092-5665-4118-bf23-daff0dbe9f3c"),
                    GuidNullableValue = Guid.Parse("46367e3b-77cd-4e4e-a931-cf8b69e84cf2"),
                    StringValue = "Lorem ipsum dolor sit amet"
                },
                new TestType
                {
                    IntValue = 0,
                    IntNullableValue = 0,
                    LongValue = 0,
                    LongNullableValue = 0,
                    DecimalValue = 0.0000M,
                    DecimalNullableValue = 0.0000M,
                    DoubleValue = 0,
                    DoubleNullableValue = 0,
                    DateTimeValue = DateTime.MinValue,
                    DateTimeNullableValue = DateTime.MinValue,
                    DateTimeOffsetValue = DateTimeOffset.MinValue,
                    DateTimeOffsetNullableValue = DateTimeOffset.MinValue,
                    GuidValue = Guid.Empty,
                    GuidNullableValue = Guid.Empty,
                    StringValue = ""
                },
                new TestType
                {
                    DecimalValue = 0.0000M
                }
            };

            var actual = await _db
                .AsQueryableValues(expected)
                .ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }


        [Fact]
        public async Task MustMatchSequenceOfTestTypeUsesConfiguration()
        {
            var expected = new[]
            {
                new TestType
                {
                    IntValue = int.MinValue,
                    IntNullableValue = int.MaxValue,
                    LongValue = long.MinValue,
                    LongNullableValue = long.MaxValue,
                    DecimalValue = 1.12M,
                    DecimalNullableValue = 999999.1234M,
                    DoubleValue = 1.1234D,
                    DoubleNullableValue = 999999.1234D,
                    DateTimeValue = DateTime.MinValue,
                    DateTimeNullableValue = DateTime.MaxValue,
                    DateTimeOffsetValue = DateTimeOffset.MinValue,
                    DateTimeOffsetNullableValue = DateTimeOffset.MaxValue,
                    GuidValue = Guid.Parse("22ebe092-5665-4118-bf23-daff0dbe9f3c"),
                    GuidNullableValue = Guid.Parse("46367e3b-77cd-4e4e-a931-cf8b69e84cf2"),
                    StringValue = "Lorem ipsum dolor sit amet",
                    StringUnicodeValue = "😀👋"
                },
                new TestType
                {
                    IntValue = 0,
                    IntNullableValue = 0,
                    LongValue = 0,
                    LongNullableValue = 0,
                    DecimalValue = 0.00M,
                    DecimalNullableValue = 0.0000M,
                    DoubleValue = 0,
                    DoubleNullableValue = 0,
                    DateTimeValue = DateTime.MinValue,
                    DateTimeNullableValue = DateTime.MinValue,
                    DateTimeOffsetValue = DateTimeOffset.MinValue,
                    DateTimeOffsetNullableValue = DateTimeOffset.MinValue,
                    GuidValue = Guid.Empty,
                    GuidNullableValue = Guid.Empty,
                    StringValue = ""
                },
                new TestType
                {
                    DecimalValue = 0.00M
                }
            };

            var actual = await _db
                .AsQueryableValues(expected, configure =>
                {
                    configure.Property(p => p.DecimalValue).NumberOfDecimals(2);
                    configure.Property(p => p.StringUnicodeValue).IsUnicode(true);
                })
                .ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithInt32()
        {
            {
                var values = new[]
                {
                    new { Id = 1 },
                    new { Id = 2 }
                };

                var expected = new[]
                {
                    int.MinValue,
                    0
                };

                var query =
                    from td in _db.TestData
                    join v in _db.AsQueryableValues(values) on td.Id equals v.Id
                    orderby td.Id
                    select td.Int32Value;

                var actual = await query.ToListAsync();

                TestUtil.EqualShape(expected, actual);
            }

            {
                var values = new[]
                {
                    new { Id = 3, Value = int.MaxValue },
                    new { Id = 2, Value = 0 }
                };

                var expected = new[] { 2, 3 };

                var query =
                    from td in _db.TestData
                    join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.Int32Value } equals new { v.Id, v.Value }
                    orderby td.Id
                    select td.Id;

                var actual = await query.ToListAsync();

                TestUtil.EqualShape(expected, actual);
            }
        }

        [Fact]
        public async Task JoinWithInt64()
        {
            var values = new[]
            {
                    new { Id = 1, Value = long.MinValue},
                    new { Id = 3, Value = long.MaxValue }
                };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.Int64Value } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithDecimal()
        {
            var values = new[]
            {
                new { Id = 1, Value = -1234567.890123M },
                new { Id = 3, Value = 1234567.890123M }
            };

            var expected = new[] { 1, 3 };

            var queryableValues = _db.AsQueryableValues(
                values,
                c => c.Property(p => p.Value).NumberOfDecimals(6)
                );

            var query =
                from td in _db.TestData
                join v in queryableValues on new { td.Id, Value = td.DecimalValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public void MustPassPropertyConfigurationOnDecimal()
        {
            var values = Enumerable.Range(0, 10)
                .Select(id => new { Id = (decimal)id });

            for (int numberOfDecimals = 0; numberOfDecimals <= 38; numberOfDecimals++)
            {
                _ = _db.AsQueryableValues(values, c => c.DefaultForNumberOfDecimals(numberOfDecimals));
                _ = _db.AsQueryableValues(values, c => c.Property(p => p.Id).NumberOfDecimals(numberOfDecimals));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(39)]
        public void MustFailPropertyConfigurationOnDecimal(int numberOfDecimals)
        {
            var values = Enumerable.Range(0, 10)
                .Select(id => new { Id = (decimal)id });

            Assert.Throws<ArgumentException>(nameof(numberOfDecimals), () =>
            {
                _ = _db.AsQueryableValues(values, c => c.DefaultForNumberOfDecimals(numberOfDecimals));
            });

            Assert.Throws<ArgumentException>(nameof(numberOfDecimals), () =>
            {
                _ = _db.AsQueryableValues(values, c => c.Property(p => p.Id).NumberOfDecimals(numberOfDecimals));
            });
        }

        [Fact]
        public void MustFailPropertyConfigurationOnNonDecimal()
        {
            var values = Enumerable.Range(0, 10)
                .Select(id => new { Id = id });

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = _db.AsQueryableValues(values, c => c.Property(p => p.Id).NumberOfDecimals(4));
            });
        }


        [Fact]
        public async Task JoinWithDouble()
        {
            var values = new[]
            {
                new { Id = 1, Value = -1234567.890123D },
                new { Id = 3, Value = 1234567.890123D }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.DoubleValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithDateTime()
        {
            var values = new[]
            {
                new { Id = 1, Value = DateTime.MinValue },
                new { Id = 3, Value = DateTime.MaxValue }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.DateTimeValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithDateTimeOffset()
        {
            var values = new[]
            {
                new { Id = 1, Value = DateTimeOffset.MinValue },
                new { Id = 2, Value = new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.FromHours(5)) }
            };

            var expected = new[] { 1, 2 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.DateTimeOffsetValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithGuid()
        {
            var values = new[]
            {
                new { Id = 1, Value = Guid.Empty },
                new { Id = 3, Value = Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6") }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.GuidValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithString()
        {
            {
                var values = new[]
                {
                    new { Id = 1, Value = "Hola!" },
                    new { Id = 3, Value = "Hi!" }
                };

                var expected = new[] { 1, 3 };

                {
                    var query =
                        from td in _db.TestData
                        join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.StringValue } equals new { v.Id, v.Value }
                        orderby td.Id
                        select td.Id;

                    var actual = await query.ToListAsync();

                    TestUtil.EqualShape(expected, actual);
                }

                {
                    var query =
                        from td in _db.TestData
                        join v in _db.AsQueryableValues(values, c => c.Property(p => p.Value).IsUnicode(true)) on new { td.Id, Value = td.StringValue } equals new { v.Id, v.Value }
                        orderby td.Id
                        select td.Id;

                    var actual = await query.ToListAsync();

                    TestUtil.EqualShape(expected, actual);
                }
            }

            {
                var values = new[]
                {
                    new { Id = 1, Value = "👋" },
                    new { Id = 2, Value = "你好！" }
                };

                var expected = new[] { 1, 2 };

                var query =
                    from td in _db.TestData
                    join v in _db.AsQueryableValues(values, c => c.DefaultForIsUnicode(true)) on new { td.Id, Value = td.UnicodeStringValue } equals new { v.Id, v.Value }
                    orderby td.Id
                    select td.Id;

                var actual = await query.ToListAsync();

                TestUtil.EqualShape(expected, actual);
            }
        }

        [Fact]
        public void MustFailPropertyConfigurationOnNonString()
        {
            var values = Enumerable.Range(0, 10)
                .Select(id => new { Id = id });

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = _db.AsQueryableValues(values, c => c.Property(p => p.Id).IsUnicode(true));
            });
        }

        [Fact]
        public async Task ComplexyQueryCase()
        {
            static IEnumerable<int> getIds()
            {
                for (int i = 1; i <= 3; i++)
                {
                    yield return i;
                }
            }

            var data1 = getIds()
                .Select(i => new { Id = i });

            var data2 = new[]
            {
                new { Greeting = "你好！" },
                new { Greeting = "Hola!" },
                new { Greeting = "Buongiorno!" }
            };

            var data3 = new[]
            {
                new { Guid = (Guid?)null },
                new { Guid = (Guid?)Guid.Parse("df2c9bfe-9d83-4331-97ce-2876d5dc6576") } ,
                new { Guid = (Guid?)Guid.Empty }
            };

            var guidsQuery = _db.AsQueryableValues(data3);

            var query =
                from td in _db.TestData
                join id in _db.AsQueryableValues(data1) on td.Id equals id.Id
                join greeting in _db.AsQueryableValues(data2, c => c.DefaultForIsUnicode(true)) on td.UnicodeStringValue equals greeting.Greeting
                where guidsQuery.Select(i => i.Guid).Contains(td.GuidValue)
                select td.Id;

            var actual = await query.ToListAsync();
            var expected = new[] { 2 };

            Assert.Equal(expected, actual);
        }
    }
}
#endif