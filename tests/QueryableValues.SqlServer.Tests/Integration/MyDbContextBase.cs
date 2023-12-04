#if TESTS
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace BlazarTech.QueryableValues.SqlServer.Tests.Integration
{
    public abstract class MyDbContextBase : DbContext
    {
        private readonly string _databaseName;
        private readonly bool _useQueryableValues;
        private readonly bool _useSelectTopOptimization;
        private readonly bool _useUseDeferredEnumeration;

#if !EFCORE3
        public event Action<string>? LogEntryEmitted;
#endif

        public DbSet<TestDataEntity> TestData { get; set; } = null!;
        public DbSet<ChildEntity> ChildEntity { get; set; } = null!;

        public MyDbContextBase(
            string databaseName,
            bool useQueryableValues = true,
            bool useSelectTopOptimization = true,
            bool useUseDeferredEnumeration = true
            )
        {
            _databaseName = databaseName;
            _useQueryableValues = useQueryableValues;
            _useSelectTopOptimization = useSelectTopOptimization;
            _useUseDeferredEnumeration = useUseDeferredEnumeration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var databaseFilePath = Path.Combine(Path.GetTempPath(), $"{_databaseName}.mdf");

#if !EFCORE3
            optionsBuilder.LogTo(
                logEntry =>
                {
                    LogEntryEmitted?.Invoke(logEntry);
                },
                Microsoft.Extensions.Logging.LogLevel.Information);
#endif

            optionsBuilder.UseSqlServer(
                @$"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Connection Timeout=190;Database={_databaseName};AttachDbFileName={databaseFilePath}",
                sqlServerOptionsBuilder =>
                {
                    if (_useQueryableValues)
                    {
                        var applyOptions = !_useSelectTopOptimization || !_useUseDeferredEnumeration;

                        if (applyOptions)
                        {
                            sqlServerOptionsBuilder.UseQueryableValues(options =>
                            {
                                if (!_useSelectTopOptimization)
                                {
                                    options.UseSelectTopOptimization(false);
                                }

#if !EFCORE3
                                if (!_useUseDeferredEnumeration)
                                {
                                    options.UseDeferredEnumeration(false);
                                }
#endif
                            });
                        }
                        else
                        {
                            sqlServerOptionsBuilder.UseQueryableValues();
                        }
                    }
                }
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder.Entity<TestDataEntity>(entity =>
            {
#if EFCORE3
                entity.Property(p => p.DecimalValue).HasColumnType("decimal(18, 6)");
#else
                entity.Property(p => p.DecimalValue).HasPrecision(18, 6);
#endif
                entity.Property(p => p.CharValue)
                    .IsUnicode(false);

                entity.Property(p => p.CharUnicodeValue)
                    .IsUnicode(true);

                entity.Property(p => p.StringValue)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(p => p.StringUnicodeValue)
                    .HasMaxLength(50)
                    .IsUnicode(true);

                entity.HasMany(p => p.ChildEntity);
            });

            modelBuilder.Entity<ChildEntity>(entity =>
            {
                entity.HasKey(p => p.Id);
            });
        }
    }

    public class TestDataEntity
    {
        public int Id { get; set; }
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public short Int16Value { get; set; }
        public int Int32Value { get; set; }
        public long Int64Value { get; set; }
        public decimal DecimalValue { get; set; }
        public float SingleValue { get; set; }
        public double DoubleValue { get; set; }
        public char CharValue { get; set; }
        public char CharUnicodeValue { get; set; }
        public string StringValue { get; set; } = null!;
        public string StringUnicodeValue { get; set; } = null!;
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public Guid GuidValue { get; set; }
        public TestEnum EnumValue { get; set; }
        public ICollection<ChildEntity> ChildEntity { get; set; } = default!;
#if EFCORE8
        public DateOnly DateOnlyValue { get; set; }
        public TimeOnly TimeOnlyValue { get; set; }
#endif
    }

    public class ChildEntity
    {
        public int Id { get; set; }
        public int TestDataEntityId { get; set; }
    }

    public enum TestEnum
    {
        None = 0,
        Value1 = 1,
        Value2 = 2,
        Value3 = 3,
        Value1000 = 1000
    }
}
#endif