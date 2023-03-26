using BlazarTech.QueryableValues;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QueryableValues.SqlServer.Benchmarks
{
    public class MyDbContext : DbContext
    {
        private readonly SqlServerSerialization _serializationOption;

        public DbSet<Int32Entity> Int32Entities { get; set; } = default!;
        public DbSet<GuidEntity> GuidEntities { get; set; } = default!;
        public DbSet<StringEntity> StringEntities { get; set; } = default!;

        public MyDbContext(SqlServerSerialization serializationOption)
        {
            _serializationOption = serializationOption;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=.\SQLEXPRESS;Integrated Security=true;Database=QueryableValuesBenchmarks;Encrypt=False;",
                builder => builder.UseQueryableValues(options => options.Serialization(_serializationOption))
                );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
        }
    }

    public class Int32Entity
    {
        public int Id { get; set; }
    }

    public class GuidEntity
    {
        public Guid Id { get; set; }
    }

    public class StringEntity
    {
        [MaxLength(100)]
        public string Id { get; set; } = default!;
    }
}
