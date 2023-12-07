#if TESTS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    public class DbContextFixture : IDisposable, IAsyncLifetime
    {
        public MyDbContext Db { get; }

        public DbContextFixture()
        {
            Db = new MyDbContext();
        }

        public void Dispose()
        {
        }

        public async Task InitializeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.Database.EnsureCreatedAsync();
            await Seed();
        }

        private async Task Seed()
        {
            var dateTimeOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, 0, TimeSpan.FromHours(5));

            var data = new[]
            {
                new TestDataEntity
                {
                    BoolValue = false,
                    ByteValue = byte.MinValue,
                    Int16Value = short.MinValue,
                    Int32Value = int.MinValue,
                    Int64Value = long.MinValue,
                    DecimalValue = -1234567.890123M,
                    SingleValue = -3.402823E+38F,
                    DoubleValue = -1234567.890123D,
                    CharValue = 'A',
                    CharUnicodeValue = '\u2603',
                    StringValue = "Hola!",
                    StringUnicodeValue = "👋",
                    DateTimeValue = DateTime.MinValue,
                    DateTimeOffsetValue = DateTimeOffset.MinValue,
                    GuidValue = Guid.Empty,
                    ChildEntity = new List<ChildEntity>
                    {
                        new ChildEntity(),
                        new ChildEntity()
                    },
#if EFCORE8
                    DateOnlyValue = DateOnly.MinValue,
                    TimeOnlyValue = TimeOnly.MinValue,
#endif
                },
                new TestDataEntity
                {
                    Int32Value = 0,
                    Int64Value = 0,
                    DecimalValue = 0,
                    SingleValue = 12345.67F,
                    DoubleValue = 0,
                    StringValue = "Hallo!",
                    StringUnicodeValue = "你好！",
                    DateTimeValue = dateTimeOffset.DateTime,
                    DateTimeOffsetValue = dateTimeOffset,
                    GuidValue = Guid.Parse("df2c9bfe-9d83-4331-97ce-2876d5dc6576"),
                    EnumValue = TestEnum.Value1000,
#if EFCORE8
                    DateOnlyValue = DateOnly.FromDateTime(dateTimeOffset.DateTime),
                    TimeOnlyValue = TimeOnly.FromDateTime(dateTimeOffset.DateTime),
#endif
                },
                new TestDataEntity
                {
                    BoolValue = true,
                    ByteValue = byte.MaxValue,
                    Int16Value = short.MaxValue,
                    Int32Value = int.MaxValue,
                    Int64Value = long.MaxValue,
                    DecimalValue = 1234567.890123M,
                    SingleValue = 3.402823E+38F,
                    DoubleValue = 1234567.890123D,
                    CharValue = 'c',
                    CharUnicodeValue = '\u2622',
                    StringValue = "Hi!",
                    StringUnicodeValue = "أهلا",
                    DateTimeValue = DateTime.MaxValue,
                    DateTimeOffsetValue = DateTimeOffset.MaxValue,
                    GuidValue = Guid.Parse("f6379213-750f-42df-91b9-73756f28c4b6"),
                    EnumValue = TestEnum.Value3,
                    ChildEntity = new List<ChildEntity>
                    {
                        new ChildEntity()
                    },
#if EFCORE8
                    DateOnlyValue = DateOnly.MaxValue,
                    TimeOnlyValue = TimeOnly.MaxValue,
#endif
                }
            };

            Db.TestData.AddRange(data);

            await Db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            Db.Dispose();
        }
    }

    [CollectionDefinition("DbContext", DisableParallelization = true)]
    public class DbContextCollection : ICollectionFixture<DbContextFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
#endif