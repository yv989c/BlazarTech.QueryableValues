using System.Collections.Generic;

namespace BlazarTech.QueryableValues.Serializers
{
    internal interface ISerializer
    {
        string Serialize<T>(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> propertyMappings)
            where T : notnull;
    }
}
