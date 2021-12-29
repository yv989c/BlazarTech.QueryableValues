using System;
using System.Collections;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal static class Extensions
    {
        // Mostly from: https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/System.Linq/src/System/Linq/Count.cs#L95
        public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

#if NET6_0_OR_GREATER
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
            // I added this... may be there's a reason why the official TryGetNonEnumeratedCount method does not do this.
            // todo: research.
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
