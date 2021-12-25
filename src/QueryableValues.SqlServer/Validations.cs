using System;

namespace BlazarTech.QueryableValues
{
    internal static class Validations
    {
        public static void ValidateNumberOfDecimals(int numberOfDecimals)
        {
            if (numberOfDecimals < 0)
            {
                throw new ArgumentException("Cannot be negative.", nameof(numberOfDecimals));
            }

            if (numberOfDecimals > 38)
            {
                throw new ArgumentException("Cannot be greater than 38.", nameof(numberOfDecimals));
            }
        }
    }
}
