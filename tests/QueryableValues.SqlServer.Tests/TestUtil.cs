using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues.SqlServer.Tests
{
    internal static class TestUtil
    {
        public static IEnumerable<decimal> GetSequenceOfDecimals(int numberOfDecimals)
        {
            var fractions = new List<decimal>() { 0 };

            var v = 1M;

            for (int i = 0; i < numberOfDecimals; i++)
            {
                fractions.Add(1 / (v *= 10));
            }

            yield return truncate(-123456.123456M);
            yield return truncate(123456.123456M);
            yield return truncate(-999_999_999_999.999999M);
            yield return truncate(999_999_999_999.999999M);

            for (decimal i = 0; i <= 1_000_000; i *= 10)
            {
                foreach (var f in fractions)
                {
                    var n = i + f;

                    yield return n; ;

                    if (n > 0)
                    {
                        yield return -n;
                    }
                }

                if (i == 0)
                {
                    i = 1;
                }
            }

            decimal truncate(decimal value)
            {
                var step = (decimal)Math.Pow(10, numberOfDecimals);
                return Math.Truncate(step * value) / step;
            }
        }
    }
}
