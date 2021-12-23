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
                    join v in _db.AsQueryableValues(values) on new { td.Id, td.Int32Value } equals new { v.Id, Int32Value = v.Value }
                    orderby td.Id
                    select td.Id;

                var actual = await query.ToListAsync();

                TestUtil.EqualShape(expected, actual);
            }
        }

        //[Fact]
        //public async Task MustMatchSequenceOfDecimal()
        //{
        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(0).ToList();
        //        var actual = await _db.AsQueryableValues(expected, 0).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }

        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(2).ToList();
        //        var actual = await _db.AsQueryableValues(expected, 2).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }

        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(6).ToList();
        //        var actual = await _db.AsQueryableValues(expected, 6).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }
        //}

        //[Fact]
        //public async Task MustFailSequenceOfDecimalInvalidNumberOfDecimals()
        //{
        //    await Assert.ThrowsAsync<ArgumentException>("numberOfDecimals", async () =>
        //    {
        //        _ = await _db.AsQueryableValues(Array.Empty<decimal>(), -1).ToListAsync();
        //    });

        //    await Assert.ThrowsAsync<ArgumentException>("numberOfDecimals", async () =>
        //    {
        //        _ = await _db.AsQueryableValues(Array.Empty<decimal>(), 39).ToListAsync();
        //    });
        //}


        //[Fact]
        //public async Task MustMatchSequenceOfDouble()
        //{
        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(0).Select(i => (double)i).ToList();
        //        var actual = await _db.AsQueryableValues(expected).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }

        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(2).Select(i => (double)i).ToList();
        //        var actual = await _db.AsQueryableValues(expected).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }

        //    {
        //        var expected = TestUtil.GetSequenceOfDecimals(6).Select(i => (double)i).ToList();
        //        var actual = await _db.AsQueryableValues(expected).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }
        //}

        //[Fact]
        //public async Task MustMatchSequenceOfString()
        //{
        //    var values = new[] { "Test 1", "Test <2>", "Test &3", "😀", "ᴭ", "" };

        //    {
        //        var expected = new[] { "Test 1", "Test <2>", "Test &3", "??", "?", "" };
        //        var actual = await _db.AsQueryableValues(values, isUnicode: false).ToListAsync();
        //        Assert.Equal(expected, actual);
        //    }

        //    {
        //        var actual = await _db.AsQueryableValues(values, isUnicode: true).ToListAsync();
        //        Assert.Equal(values, actual);
        //    }

        //    {
        //        var actual = await _db.AsQueryableValues(Array.Empty<string>()).ToListAsync();
        //        Assert.Empty(actual);
        //    }
        //}

        //[Fact]
        //public async Task MustMatchSequenceOfDateTime()
        //{
        //    var now = DateTime.Now;

        //    now = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), now.Kind)
        //        .AddMilliseconds(123);

        //    var utcNow = DateTime.UtcNow;

        //    utcNow = new DateTime(utcNow.Ticks - (utcNow.Ticks % TimeSpan.TicksPerSecond), utcNow.Kind)
        //        .AddMilliseconds(123);

        //    var values = new[] {
        //        DateTime.MinValue,
        //        DateTime.MaxValue,
        //        DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local),
        //        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local),
        //        DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
        //        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
        //        now,
        //        utcNow
        //    };

        //    var actual = await _db.AsQueryableValues(values).ToListAsync();
        //    Assert.Equal(values, actual);
        //}

        //[Fact]
        //public async Task MustMatchSequenceOfDateTimeOffset()
        //{
        //    var values = new[] {
        //        DateTimeOffset.MinValue,
        //        DateTimeOffset.MaxValue,
        //        new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(0)),
        //        new DateTimeOffset(2021, 1, 1, 1, 2, 3, 4, TimeSpan.FromHours(5.5))
        //    };

        //    var actual = await _db.AsQueryableValues(values).ToListAsync();

        //    Assert.Equal(values, actual);
        //}

        //[Fact]
        //public async Task MustMatchSequenceOfGuid()
        //{
        //    var expected = new[] {
        //        Guid.Empty,
        //        Guid.Parse("5a9354e2-ba25-4acc-a365-6a2594980879"),
        //        Guid.Empty,
        //        Guid.Parse("9816e5dc-56c1-44c5-88b8-3557c4fc55b7")
        //    };

        //    var actual = await _db.AsQueryableValues(expected).ToListAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityInt32()
        //{
        //    var values = new[] {
        //        0,
        //        int.MaxValue,
        //        int.MaxValue
        //    };

        //    var expected = new[] { 2, 3, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.Int32Value equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityInt64()
        //{
        //    var values = new[] {
        //        long.MinValue,
        //        long.MaxValue
        //    };

        //    var expected = new[] { 1, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.Int64Value equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityDecimal()
        //{
        //    var values = new[] {
        //        -1234567.890123M,
        //        1234567.890123M
        //    };

        //    var expected = new[] { 1, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values, numberOfDecimals: 6) on i.DecimalValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityDouble()
        //{
        //    var values = new[] {
        //        -1234567.890123D,
        //        1234567.890123D
        //    };

        //    var expected = new[] { 1, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.DoubleValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityString()
        //{
        //    var values = new[] {
        //        "Hi!",
        //        "Hola!"
        //    };

        //    var expected = new[] { 1, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.StringValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityUnicodeString()
        //{
        //    var values = new[] {
        //        "👋",
        //        "你好！"
        //    };

        //    var expected = new[] { 1, 2 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values, isUnicode: true) on i.UnicodeStringValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        //[Fact]
        //public async Task QueryEntityDateTime()
        //{
        //    var dateTimeOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.FromHours(5));

        //    var values = new[] {
        //        DateTime.MinValue,
        //        dateTimeOffset.DateTime
        //    };

        //    var expected = new[] { 1, 2 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.DateTimeValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}


        //[Fact]
        //public async Task QueryEntityDateTimeOffset()
        //{
        //    var dateTimeOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.FromHours(5));

        //    var values = new[] {
        //        DateTimeOffset.MinValue,
        //        dateTimeOffset
        //    };

        //    var expected = new[] { 1, 2 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.DateTimeOffsetValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}


        //[Fact]
        //public async Task QueryEntityGuid()
        //{
        //    var values = new[] {
        //        Guid.Empty,
        //        Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6")
        //    };

        //    var expected = new[] { 1, 3 };

        //    var actual = await (
        //        from i in _db.TestData
        //        join v in _db.AsQueryableValues(values) on i.GuidValue equals v
        //        orderby i.Id
        //        select i.Id
        //        )
        //        .ToArrayAsync();

        //    Assert.Equal(expected, actual);
        //}

        [Fact]
        public async Task ComplexTypeTest()
        {
            //var input = new[]
            //{
            //    new TestEntity{ Id = 1, AnotherId = 2, Greeting = "Hello" },
            //    new TestEntity{ Id = 1, OtherId = 123, AnotherId = 2 }
            //};

            var input = new[]
            {
                new TestEntityStruct{ Id = 1, AnotherId = 2, Greeting = "Hello" },
                new TestEntityStruct{ Id = 1, OtherId = 123, AnotherId = 2 }
            };

            //var input = new[]
            //{
            //    new { Id = 1, AnotherId = 2, Greeting = "Hello 1" },
            //    new { Id = 3, AnotherId = 4, Greeting = "Hello 2" }
            //};

            // Tupples not supported.
            //var input = new[]
            //{
            //    (Id: 1, AnotherId: 2, Greeting: "Hello 1"),
            //    (Id: 3, AnotherId: 4, Greeting: "Hello 2")
            //};

            //var asd2 =
            //    from i in _db.TestData
            //    select Tuple.Create(i.GuidValue, i.Id);

            //var asd2 = _db.TestData.Select(i => (A: i.GuidValue, B: i.Id));

            //_ = await asd2.ToListAsync();

            var asdasdQuery = _db.AsQueryableValues(input, options =>
            {
                //options.Property(p => p.Id).NumberOfDecimals(6);
                //options.Property(p => p.Greeting).IsUnicode();
            });
            var asdasd = await asdasdQuery.ToListAsync();

            var asd =
                from i in _db.TestData
                    //join e in _db.AsQueryableValuesTest(input) on i.Int32Value equals e.Id
                    //join e in _db.AsQueryableValuesTest(input) on new { A = i.Id, B = i.Id } equals new { A = e.Id, B = e.AnotherId }
                join e in _db.AsQueryableValues(input) on i.Id equals e.Id
                select i.GuidValue;

            _ = await asd.ToListAsync();

            for (int i = 0; i < 10; i++)
            {
                _ = await asd.ToListAsync();
            }

            //var output = await _db.AsQueryableValuesTest(input).ToListAsync();

            //var expected = System.Text.Json.JsonSerializer.Serialize(input);
            //var actual = System.Text.Json.JsonSerializer.Serialize(output);
            //Assert.Equal(expected, actual);
        }
    }
}
#endif