using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues.Serializers
{
    internal interface ISerializer
    {
        string Serialize(IEnumerable<byte> values);
        string Serialize(IEnumerable<short> values);
        string Serialize(IEnumerable<int> values);
        string Serialize(IEnumerable<long> values);
        string Serialize(IEnumerable<decimal> values);
        string Serialize(IEnumerable<float> values);
        string Serialize(IEnumerable<double> values);
        string Serialize(IEnumerable<DateTime> values);
        string Serialize(IEnumerable<DateTimeOffset> values);
        string Serialize(IEnumerable<Guid> values);
        string Serialize(IEnumerable<char> values);
        string Serialize(IEnumerable<string> values);
        string Serialize<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
            where T : notnull;
    }
}
