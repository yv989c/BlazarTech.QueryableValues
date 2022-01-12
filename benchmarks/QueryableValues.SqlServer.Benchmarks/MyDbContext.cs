using BlazarTech.QueryableValues;
using Microsoft.EntityFrameworkCore;

namespace QueryableValues.SqlServer.Benchmarks
{
    public class MyDbContext : DbContext
    {
#pragma warning disable CS8618
        public DbSet<IntEntity> IntEntities { get; set; }
        public DbSet<GuidEntity> GuidEntities { get; set; }
#pragma warning restore CS8618

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=.\SQLEXPRESS;Integrated Security=true;Database=QueryableValuesBenchmarks",
                builder => builder.UseQueryableValues()
                );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
        }
    }

    public class IntEntity
    {
        public int Id { get; set; }
    }

    public class GuidEntity
    {
        public Guid Id { get; set; }
    }
}
