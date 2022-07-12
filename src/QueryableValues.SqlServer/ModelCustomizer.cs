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

        private static EntityTypeBuilder<QueryableValuesEntity<T>> SetupEntity<T>(ModelBuilder modelBuilder)
        {
            return modelBuilder
                .Entity<QueryableValuesEntity<T>>()
                // By mapping to a fake view, we stop EF from including these entities during
                // SQL generation in migrations and by the Create and Drop apis in DbContext.Database.
                .ToView($"{nameof(QueryableValuesEntity)}{typeof(T).Name}")
                .HasNoKey();
        }

        private static void SetupEntity(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<QueryableValuesEntity>(entity =>
                {
                    SetDefaultPrecision(entity.Property(p => p.Decimal0));
                    SetDefaultPrecision(entity.Property(p => p.Decimal1));
                    SetDefaultPrecision(entity.Property(p => p.Decimal2));
                    SetDefaultPrecision(entity.Property(p => p.Decimal3));
                    SetDefaultPrecision(entity.Property(p => p.Decimal4));
                    SetDefaultPrecision(entity.Property(p => p.Decimal5));
                    SetDefaultPrecision(entity.Property(p => p.Decimal6));
                    SetDefaultPrecision(entity.Property(p => p.Decimal7));
                    SetDefaultPrecision(entity.Property(p => p.Decimal8));
                    SetDefaultPrecision(entity.Property(p => p.Decimal9));

                    // By mapping to a fake view, we stop EF from including these entities during
                    // SQL generation in migrations and by the Create and Drop apis in DbContext.Database.
                    entity
                        .ToView(nameof(QueryableValuesEntity))
                        .HasNoKey();
                });
        }

        public void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            SetupEntity<byte>(modelBuilder);
            SetupEntity<short>(modelBuilder);
            SetupEntity<int>(modelBuilder);
            SetupEntity<long>(modelBuilder);

            var decimalProperty = SetupEntity<decimal>(modelBuilder)
                .Property(p => p.V);

            SetDefaultPrecision(decimalProperty);

            SetupEntity<float>(modelBuilder);
            SetupEntity<double>(modelBuilder);
            SetupEntity<DateTime>(modelBuilder);
            SetupEntity<DateTimeOffset>(modelBuilder);
            SetupEntity<Guid>(modelBuilder);
            SetupEntity<char>(modelBuilder);
            SetupEntity<string>(modelBuilder);

            SetupEntity(modelBuilder);

            _previousModelCustomizer.Customize(modelBuilder, context);
        }
    }
}
#endif