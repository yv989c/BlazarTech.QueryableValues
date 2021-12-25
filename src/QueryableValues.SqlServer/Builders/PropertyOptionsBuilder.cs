using System;
using System.Reflection;

namespace BlazarTech.QueryableValues.Builders
{
    /// <summary>
    /// Provides APIs for configuring the behavior of a property.
    /// </summary>
    /// <typeparam name="TProperty">The property's type.</typeparam>
    public sealed class PropertyOptionsBuilder<TProperty> : IEquatable<PropertyOptionsBuilder<TProperty>>, IPropertyOptionsBuilder
    {
        private readonly MemberInfo _memberInfo;

        private bool _isUnicode;
        private int _numberOfDecimals;

        bool IPropertyOptionsBuilder.IsUnicode => _isUnicode;
        int IPropertyOptionsBuilder.NumberOfDecimals => _numberOfDecimals;

        internal PropertyOptionsBuilder(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;
        }

        /// <summary>
        /// Configures the property as capable of handling unicode characters. Can only be set on <see cref="string"/> properties.
        /// </summary>
        /// <param name="isUnicode">A value indicating whether the property can handle unicode characters.</param>
        /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public PropertyOptionsBuilder<TProperty> IsUnicode(bool isUnicode = true)
        {
            if (typeof(TProperty) != typeof(string))
            {
                throw new InvalidOperationException("This method can only be used on String properties.");
            }

            _isUnicode = isUnicode;

            return this;
        }

        // todo: consider using HasPrecision instead. With defaults for both parameters.

        /// <summary>
        /// Configures the number of decimals supported by the property. Can only be set on <see cref="decimal"/> properties.
        /// </summary>
        /// <param name="numberOfDecimals">The number of decimals supported by the property</param>
        /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public PropertyOptionsBuilder<TProperty> NumberOfDecimals(int numberOfDecimals)
        {
            if (typeof(TProperty) != typeof(decimal))
            {
                throw new InvalidOperationException("This method can only be used on Decimal properties.");
            }

            Validations.ValidateNumberOfDecimals(numberOfDecimals);

            _numberOfDecimals = numberOfDecimals;

            return this;
        }

#pragma warning disable CS1591
        public override int GetHashCode()
        {
            return HashCode.Combine(_memberInfo, _numberOfDecimals, _numberOfDecimals);
        }

        public bool Equals(PropertyOptionsBuilder<TProperty>? other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _memberInfo == other._memberInfo &&
                _isUnicode == other._isUnicode &&
                _numberOfDecimals == other._numberOfDecimals;
        }

        public override bool Equals(object? obj) => Equals(obj as PropertyOptionsBuilder<TProperty>);
#pragma warning restore CS1591
    }

    internal interface IPropertyOptionsBuilder
    {
        bool IsUnicode { get; }
        int NumberOfDecimals { get; }
    }
}
