#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        /// <summary>
        /// Used to satisfice model validation and remove the SqlServerEventId.DecimalTypeDefaultWarning warning from the logs.
        /// </summary>
        private static void SetDefaultPrecision<T>(PropertyBuilder<T> property)
        {
#if EFCORE3
            property.HasColumnType("decimal(18, 6)");
#else
            property.HasPrecision(18, 6);
#endif
        }

        private static void SetupEntity<TEntity>(ModelBuilder modelBuilder, Action<EntityTypeBuilder<TEntity>>? buildAction = null)
            where TEntity : QueryableValuesEntity
        {
            modelBuilder
                .Entity<TEntity>(entity =>
                {
                    buildAction?.Invoke(entity);

                    // By mapping to a fake view, we stop EF from including these entities during
                    // SQL generation in migrations and by the Create and Drop apis in DbContext.Database.
                    entity
                        .ToView(Guid.NewGuid().ToString())
                        .HasKey(i => i.X);
                });
        }

        public void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            SetupEntity<ComplexQueryableValuesEntity>(modelBuilder, entity =>
            {
                SetDefaultPrecision(entity.Property(p => p.M));
                SetDefaultPrecision(entity.Property(p => p.M1));
                SetDefaultPrecision(entity.Property(p => p.M2));
                SetDefaultPrecision(entity.Property(p => p.M3));
                SetDefaultPrecision(entity.Property(p => p.M4));
                SetDefaultPrecision(entity.Property(p => p.M5));
                SetDefaultPrecision(entity.Property(p => p.M6));
                SetDefaultPrecision(entity.Property(p => p.M7));
                SetDefaultPrecision(entity.Property(p => p.M8));
                SetDefaultPrecision(entity.Property(p => p.M9));
            });

            SetupEntity<SimpleQueryableValuesEntity<byte>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<short>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<int>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<long>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<decimal>>(modelBuilder, entity => SetDefaultPrecision(entity.Property(p => p.V)));
            SetupEntity<SimpleQueryableValuesEntity<float>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<double>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<DateTime>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<DateTimeOffset>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<Guid>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<char>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<string>>(modelBuilder);

#if EFCORE8
            SetupEntity<SimpleQueryableValuesEntity<DateOnly>>(modelBuilder);
            SetupEntity<SimpleQueryableValuesEntity<TimeOnly>>(modelBuilder);
#endif

            _previousModelCustomizer.Customize(modelBuilder, context);
        }
    }
}
#endif