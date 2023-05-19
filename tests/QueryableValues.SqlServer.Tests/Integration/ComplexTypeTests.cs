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
    public abstract class ComplexTypeTests
    {
        protected readonly IMyDbContext _db;

        public class TestType
        {
            public bool BooleanValue { get; set; }
            public bool? BooleanNullableValue { get; set; }
            public byte ByteValue { get; set; }
            public byte? ByteNullableValue { get; set; }
            public short Int16Value { get; set; }
            public short? Int16NullableValue { get; set; }
            public int Int32Value { get; set; }
            public int? Int32NullableValue { get; set; }
            public long Int64Value { get; set; }
            public long? Int64NullableValue { get; set; }
            public decimal DecimalValue { get; set; }
            public decimal? DecimalNullableValue { get; set; }
            public float SingleValue { get; set; }
            public float? SingleNullableValue { get; set; }
            public double DoubleValue { get; set; }
            public double? DoubleNullableValue { get; set; }
            public DateTime DateTimeValue { get; set; }
            public DateTime? DateTimeNullableValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public DateTimeOffset? DateTimeOffsetNullableValue { get; set; }
            public Guid GuidValue { get; set; }
            public Guid? GuidNullableValue { get; set; }
            public char CharValue { get; set; }
            public char? CharNullableValue { get; set; }
            public char CharUnicodeValue { get; set; }
            public char? CharUnicodeNullableValue { get; set; }
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

        [Fact]
        public async Task MustValidateEnumerationCount()
        {
            var enumerationCount = 0;

            IEnumerable<TestType> enumerableData()
            {
                enumerationCount++;

                yield return new TestType
                {
                    Int32Value = int.MinValue,
                    Int32NullableValue = int.MaxValue
                };
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
                    Int32Value = int.MinValue,
                    Int32NullableValue = int.MaxValue,
                    Int64Value = long.MinValue,
                    Int64NullableValue = long.MaxValue,
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
                    CharValue = 'A',
                    CharUnicodeValue = '1',
                    StringValue = "Lorem ipsum dolor sit amet"
                },
                new TestType
                {
                    Int32Value = 0,
                    Int32NullableValue = 0,
                    Int64Value = 0,
                    Int64NullableValue = 0,
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
                    CharValue = ' ',
                    CharUnicodeValue = ' ',
                    StringValue = ""
                },
                new TestType
                {
                    DecimalValue = 0.0000M,
                    CharValue = 'B',
                    CharUnicodeValue = '2',
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
                    Int32Value = int.MinValue,
                    Int32NullableValue = int.MaxValue,
                    Int64Value = long.MinValue,
                    Int64NullableValue = long.MaxValue,
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
                    CharValue = 'a',
                    CharUnicodeValue = '☢',
                    StringValue = "Lorem ipsum dolor sit amet",
                    StringUnicodeValue = "😀👋"
                },
                new TestType
                {
                    Int32Value = 0,
                    Int32NullableValue = 0,
                    Int64Value = 0,
                    Int64NullableValue = 0,
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
                    CharValue = 'b',
                    CharNullableValue = '1',
                    CharUnicodeValue = '☃',
                    CharUnicodeNullableValue = '☢',
                    StringValue = ""
                },
                new TestType
                {
                    DecimalValue = 0.00M,
                    CharValue = ' ',
                    CharUnicodeValue = ' '
                }
            };

            var actual = await _db
                .AsQueryableValues(expected, configure =>
                {
                    configure.Property(p => p.DecimalValue).NumberOfDecimals(2);
                    configure.Property(p => p.CharUnicodeValue).IsUnicode(true);
                    configure.Property(p => p.CharUnicodeNullableValue).IsUnicode(true);
                    configure.Property(p => p.StringUnicodeValue).IsUnicode(true);
                })
                .ToListAsync();

            TestUtil.EqualShape(expected, actual);
        }

        [Fact]
        public async Task JoinWithBoolean()
        {
            var values = new[]
            {
                new { Id = 1, Value = false },
                new { Id = 2, Value = true },
                new { Id = 3, Value = true }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.BoolValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task JoinWithByte()
        {
            var values = new[]
            {
                new { Id = 1, Value = byte.MinValue },
                new { Id = 2, Value = (byte)1 },
                new { Id = 3, Value = byte.MaxValue }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.ByteValue } equals new { v.Id, v.Value }
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task JoinWithInt16()
        {
            var values = new[]
            {
                new { Value = short.MinValue },
                new { Value = short.MaxValue }
            };

            var expected = new[] { 1, 3 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on td.Int16Value equals v.Value
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            Assert.Equal(expected, actual);
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

                Assert.Equal(expected, actual);
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

                Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
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
        public async Task JoinWithSingle()
        {
            var values = new[]
            {
                new { Value = -3.402823E+38F },
                new { Value = 12345.67F }
            };

            var expected = new[] { 1, 2 };

            var query =
                from td in _db.TestData
                join v in _db.AsQueryableValues(values) on td.SingleValue equals v.Value
                orderby td.Id
                select td.Id;

            var actual = await query.ToListAsync();

            Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
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

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task JoinWithChar()
        {
            {
                var values = new[]
                {
                    new { Id = 1, Value = 'A' },
                    new { Id = 3, Value = '☢' }
                };

                {
                    var expected = new[] { 1 };

                    var query =
                        from td in _db.TestData
                        join v in _db.AsQueryableValues(values) on new { td.Id, Value = td.CharValue } equals new { v.Id, v.Value }
                        orderby td.Id
                        select td.Id;

                    var actual = await query.ToListAsync();

                    Assert.Equal(expected, actual);
                }

                {
                    var expected = new[] { 3 };

                    var query =
                        from td in _db.TestData
                        join v in _db.AsQueryableValues(values, c => c.Property(p => p.Value).IsUnicode(true)) on new { td.Id, Value = td.CharUnicodeValue } equals new { v.Id, v.Value }
                        orderby td.Id
                        select td.Id;

                    var actual = await query.ToListAsync();

                    Assert.Equal(expected, actual);
                }
            }

            {
                var values = new[]
                {
                    new { Id = 1, Value = '☃' },
                    new { Id = 3, Value = '☢' }
                };

                var expected = new[] { 1, 3 };

                var query =
                    from td in _db.TestData
                    join v in _db.AsQueryableValues(values, c => c.DefaultForIsUnicode(true)) on new { td.Id, Value = td.CharUnicodeValue } equals new { v.Id, v.Value }
                    orderby td.Id
                    select td.Id;

                var actual = await query.ToListAsync();

                Assert.Equal(expected, actual);
            }
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

                    Assert.Equal(expected, actual);
                }

                {
                    var query =
                        from td in _db.TestData
                        join v in _db.AsQueryableValues(values, c => c.Property(p => p.Value).IsUnicode(true)) on new { td.Id, Value = td.StringValue } equals new { v.Id, v.Value }
                        orderby td.Id
                        select td.Id;

                    var actual = await query.ToListAsync();

                    Assert.Equal(expected, actual);
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
                    join v in _db.AsQueryableValues(values, c => c.DefaultForIsUnicode(true)) on new { td.Id, Value = td.StringUnicodeValue } equals new { v.Id, v.Value }
                    orderby td.Id
                    select td.Id;

                var actual = await query.ToListAsync();

                Assert.Equal(expected, actual);
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
                join greeting in _db.AsQueryableValues(data2, c => c.DefaultForIsUnicode(true)) on td.StringUnicodeValue equals greeting.Greeting
                where guidsQuery.Select(i => i.Guid).Contains(td.GuidValue)
                select td.Id;

            var actual = await query.ToListAsync();
            var expected = new[] { 2 };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MustBeEmpty()
        {
            var actual = await _db.AsQueryableValues(Array.Empty<TestType>()).ToListAsync();
            Assert.Empty(actual);
        }

        [Fact]
        public async Task MustMatchCount()
        {
            const int expectedItemCount = 1000;

            var values = Enumerable.Range(0, expectedItemCount)
                .Select(i => new
                {
                    Id = i
                });

            var actualItemCount = await _db.AsQueryableValues(values).CountAsync();

            Assert.Equal(expectedItemCount, actualItemCount);
        }

        [Fact]
        public async Task JoinWithInclude()
        {
            var values = new[]
            {
                new { Id = 100, Value = Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6") },
                new { Id = 300, Value = Guid.Empty }
            };

            var query =
                from td in _db.TestData.AsNoTracking().Include(p => p.ChildEntity)
                join v in _db.AsQueryableValues(values) on td.GuidValue equals v.Value
                orderby td.Id
                select td;

            var actual = await query.ToListAsync();
            
            Assert.Equal(2, actual.Count);

            Assert.Equal(1, actual[0].Id);
            Assert.Equal(2, actual[0].ChildEntity.Count);
            
            Assert.Equal(3, actual[1].Id);
            Assert.Equal(1, actual[1].ChildEntity.Count);
        }

        [Fact]
        public async Task JoinWithIncludeAndTake()
        {
            var values = new[]
            {
                new { Id = 100, Value = Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6") },
                new { Id = 300, Value = Guid.Empty }
            };

            var query =
                from td in _db.TestData.AsNoTracking().Include(p => p.ChildEntity)
                join v in _db.AsQueryableValues(values).Take(1) on td.GuidValue equals v.Value
                orderby td.Id
                select td;

            var actual = await query.ToListAsync();

            Assert.Single(actual);

            Assert.Equal(3, actual[0].Id);
            Assert.Equal(1, actual[0].ChildEntity.Count);
        }

        [Fact]
        public async Task JoinWithIncludeAndSkip()
        {
            var values = new[]
            {
                new { Id = 100, Value = Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6") },
                new { Id = 300, Value = Guid.Empty }
            };

            var query =
                from td in _db.TestData.AsNoTracking().Include(p => p.ChildEntity)
                join v in _db.AsQueryableValues(values).Skip(1) on td.GuidValue equals v.Value
                orderby td.Id
                select td;

            var actual = await query.ToListAsync();

            Assert.Single(actual);

            Assert.Equal(1, actual[0].Id);
            Assert.Equal(2, actual[0].ChildEntity.Count);
        }
    }

    [Collection("DbContext")]
    public class JsonComplexTypeTests : ComplexTypeTests
    {
        public JsonComplexTypeTests(DbContextFixture contextFixture) : base(contextFixture)
        {
            _db.Options.Serialization(SqlServerSerialization.UseJson);
        }
    }

    [Collection("DbContext")]
    public class XmlComplexTypeTests : ComplexTypeTests
    {
        public XmlComplexTypeTests(DbContextFixture contextFixture) : base(contextFixture)
        {
            _db.Options.Serialization(SqlServerSerialization.UseXml);
        }
    }
}
#endif