using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal class ValuesWrapper<T, T2>
    {
        public IEnumerable<T> OriginalValues { get; }
        public IEnumerable<T2> ProjectedValues { get; }

        public ValuesWrapper(IEnumerable<T> originalValues, IEnumerable<T2> projectedValues)
        {
            OriginalValues = originalValues;
            ProjectedValues = projectedValues;
        }
    }
}
