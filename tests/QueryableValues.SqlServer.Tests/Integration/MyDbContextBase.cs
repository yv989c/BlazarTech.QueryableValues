#if TESTS
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    public class MyDbContextBase : DbContext
    {
        private readonly string _databaseName;

#nullable disable
        public DbSet<TestDataEntity> TestData { get; set; }
#nullable restore

        public MyDbContextBase(string databaseName)
        {
            _databaseName = databaseName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var databaseFilePath = Path.Combine(Path.GetTempPath(), $"{_databaseName}.mdf");

            optionsBuilder.UseSqlServer(
                @$"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Connection Timeout=190;Database={_databaseName};AttachDbFileName={databaseFilePath}",
                sqlServerOptionsBuilder =>
                {
                    sqlServerOptionsBuilder.UseQueryableValues();
                }
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestDataEntity>(entity =>
            {
#if EFCORE3
                entity.Property(p => p.DecimalValue).HasColumnType("decimal(18, 6)");
#else
                entity.Property(p => p.DecimalValue).HasPrecision(18, 6);
#endif
                entity.Property(p => p.StringValue)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(p => p.UnicodeStringValue)
                    .HasMaxLength(50)
                    .IsUnicode(true);
            });
        }
    }

#nullable disable
    public class TestDataEntity
    {
        public int Id { get; set; }
        public int Int32Value { get; set; }
        public long Int64Value { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public string StringValue { get; set; }
        public string UnicodeStringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
    }
#nullable restore
}
#endif