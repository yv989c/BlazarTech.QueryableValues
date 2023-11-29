using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazarTech.QueryableValues
{
    internal abstract class QueryableValuesEntity
    {
        public const string IndexPropertyName = nameof(X);

        public int X { get; set; }
    }

    internal class SimpleQueryableValuesEntity<T> : QueryableValuesEntity
    {
        public T? V { get; set; }
    }

    internal class ComplexQueryableValuesEntity : QueryableValuesEntity
    {
        private static readonly IReadOnlyList<string> DataPropertyNames;

        static ComplexQueryableValuesEntity()
        {
            DataPropertyNames = GetDataPropertyNames();
        }

        public int? I { get; set; }
        public int? I1 { get; set; }
        public int? I2 { get; set; }
        public int? I3 { get; set; }
        public int? I4 { get; set; }
        public int? I5 { get; set; }
        public int? I6 { get; set; }
        public int? I7 { get; set; }
        public int? I8 { get; set; }
        public int? I9 { get; set; }

        public long? L { get; set; }
        public long? L1 { get; set; }
        public long? L2 { get; set; }
        public long? L3 { get; set; }
        public long? L4 { get; set; }
        public long? L5 { get; set; }
        public long? L6 { get; set; }
        public long? L7 { get; set; }
        public long? L8 { get; set; }
        public long? L9 { get; set; }

        public decimal? M { get; set; }
        public decimal? M1 { get; set; }
        public decimal? M2 { get; set; }
        public decimal? M3 { get; set; }
        public decimal? M4 { get; set; }
        public decimal? M5 { get; set; }
        public decimal? M6 { get; set; }
        public decimal? M7 { get; set; }
        public decimal? M8 { get; set; }
        public decimal? M9 { get; set; }

        public double? D { get; set; }
        public double? D1 { get; set; }
        public double? D2 { get; set; }
        public double? D3 { get; set; }
        public double? D4 { get; set; }
        public double? D5 { get; set; }
        public double? D6 { get; set; }
        public double? D7 { get; set; }
        public double? D8 { get; set; }
        public double? D9 { get; set; }

        public DateTime? A { get; set; }
        public DateTime? A1 { get; set; }
        public DateTime? A2 { get; set; }
        public DateTime? A3 { get; set; }
        public DateTime? A4 { get; set; }
        public DateTime? A5 { get; set; }
        public DateTime? A6 { get; set; }
        public DateTime? A7 { get; set; }
        public DateTime? A8 { get; set; }
        public DateTime? A9 { get; set; }

        public DateTimeOffset? E { get; set; }
        public DateTimeOffset? E1 { get; set; }
        public DateTimeOffset? E2 { get; set; }
        public DateTimeOffset? E3 { get; set; }
        public DateTimeOffset? E4 { get; set; }
        public DateTimeOffset? E5 { get; set; }
        public DateTimeOffset? E6 { get; set; }
        public DateTimeOffset? E7 { get; set; }
        public DateTimeOffset? E8 { get; set; }
        public DateTimeOffset? E9 { get; set; }

        public Guid? G { get; set; }
        public Guid? G1 { get; set; }
        public Guid? G2 { get; set; }
        public Guid? G3 { get; set; }
        public Guid? G4 { get; set; }
        public Guid? G5 { get; set; }
        public Guid? G6 { get; set; }
        public Guid? G7 { get; set; }
        public Guid? G8 { get; set; }
        public Guid? G9 { get; set; }

        public string? S { get; set; }
        public string? S1 { get; set; }
        public string? S2 { get; set; }
        public string? S3 { get; set; }
        public string? S4 { get; set; }
        public string? S5 { get; set; }
        public string? S6 { get; set; }
        public string? S7 { get; set; }
        public string? S8 { get; set; }
        public string? S9 { get; set; }

        public bool? B { get; set; }
        public bool? B1 { get; set; }
        public bool? B2 { get; set; }
        public bool? B3 { get; set; }
        public bool? B4 { get; set; }
        public bool? B5 { get; set; }
        public bool? B6 { get; set; }
        public bool? B7 { get; set; }
        public bool? B8 { get; set; }
        public bool? B9 { get; set; }

        public byte? Y { get; set; }
        public byte? Y1 { get; set; }
        public byte? Y2 { get; set; }
        public byte? Y3 { get; set; }
        public byte? Y4 { get; set; }
        public byte? Y5 { get; set; }
        public byte? Y6 { get; set; }
        public byte? Y7 { get; set; }
        public byte? Y8 { get; set; }
        public byte? Y9 { get; set; }

        public short? H { get; set; }
        public short? H1 { get; set; }
        public short? H2 { get; set; }
        public short? H3 { get; set; }
        public short? H4 { get; set; }
        public short? H5 { get; set; }
        public short? H6 { get; set; }
        public short? H7 { get; set; }
        public short? H8 { get; set; }
        public short? H9 { get; set; }

        public float? F { get; set; }
        public float? F1 { get; set; }
        public float? F2 { get; set; }
        public float? F3 { get; set; }
        public float? F4 { get; set; }
        public float? F5 { get; set; }
        public float? F6 { get; set; }
        public float? F7 { get; set; }
        public float? F8 { get; set; }
        public float? F9 { get; set; }

        public char? C { get; set; }
        public char? C1 { get; set; }
        public char? C2 { get; set; }
        public char? C3 { get; set; }
        public char? C4 { get; set; }
        public char? C5 { get; set; }
        public char? C6 { get; set; }
        public char? C7 { get; set; }
        public char? C8 { get; set; }
        public char? C9 { get; set; }

        private static List<string> GetDataPropertyNames()
        {
            var properties = typeof(ComplexQueryableValuesEntity).GetProperties();
            var result = new List<string>(properties.Length);

            foreach (var property in properties)
            {
                if (property.Name is IndexPropertyName)
                {
                    continue;
                }

                result.Add(property.Name);
            }

            return result;
        }

        internal static IEnumerable<string> GetUnmappedPropertyNames(IReadOnlyList<EntityPropertyMapping> mappings)
        {
            var targetProperties = new HashSet<string>(mappings.Select(i => i.Target.Name));

            foreach (var propertyName in DataPropertyNames)
            {
                if (targetProperties.Contains(propertyName))
                {
                    continue;
                }

                yield return propertyName;
            }
        }
    }
}