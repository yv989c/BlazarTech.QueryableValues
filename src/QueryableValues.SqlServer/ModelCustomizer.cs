#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace BlazarTech.QueryableValues
{
    internal sealed class ModelCustomizer<TPreviousModelCustomizer> : IModelCustomizer
        where TPreviousModelCustomizer : IModelCustomizer
    {
        private readonly TPreviousModelCustomizer _previousModelCustomizer;

        public ModelCustomizer(TPreviousModelCustomizer previousModelCustomizer)
        {
            _previousModelCustomizer = previousModelCustomizer;
        }

        private static void SetupEntity<T>(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<QueryableValuesEntity<T>>()
                // By mapping to a fake view, we stop EF from including these entities during
                // SQL generation in migrations and by the Create and Drop apis in DbContext.Database.
                .ToView($"{nameof(QueryableValuesEntity)}{typeof(T).Name}")
                .HasNoKey();
        }

        private static void SetupEntity(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<QueryableValuesEntity>()
                // By mapping to a fake view, we stop EF from including these entities during
                // SQL generation in migrations and by the Create and Drop apis in DbContext.Database.
                .ToView(nameof(QueryableValuesEntity))
                .HasNoKey();
        }

        public void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            SetupEntity<int>(modelBuilder);
            SetupEntity<long>(modelBuilder);
            SetupEntity<decimal>(modelBuilder);
            SetupEntity<double>(modelBuilder);
            SetupEntity<DateTime>(modelBuilder);
            SetupEntity<DateTimeOffset>(modelBuilder);
            SetupEntity<string>(modelBuilder);
            SetupEntity<Guid>(modelBuilder);

            SetupEntity(modelBuilder);

            _previousModelCustomizer.Customize(modelBuilder, context);
        }
    }
}
#endif