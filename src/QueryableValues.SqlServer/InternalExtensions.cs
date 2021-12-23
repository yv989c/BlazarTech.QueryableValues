using System;
using System.Collections;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal static class InternalExtensions
    {
        public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Target multiple runtimes and check for NET6 instead of EFCORE6.
#if EFCORE6
            if (System.Linq.Enumerable.TryGetNonEnumeratedCount(source, out count))
            {
                return true;
            }
#else
            if (source is ICollection<TSource> collectionoft)
            {
                count = collectionoft.Count;
                return true;
            }

            if (source is ICollection collection)
            {
                count = collection.Count;
                return true;
            }
#endif

            if (source is IReadOnlyCollection<TSource> readOnlyCollection)
            {
                count = readOnlyCollection.Count;
                return true;
            }

            count = 0;

            return false;
        }
    }
}
