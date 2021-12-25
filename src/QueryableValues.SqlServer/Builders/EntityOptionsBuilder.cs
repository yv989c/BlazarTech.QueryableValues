using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlazarTech.QueryableValues.Builders
{
    /// <summary>
    /// Provides APIs for configuring the behavior of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to be projected.</typeparam>
    public sealed class EntityOptionsBuilder<T> : IEquatable<EntityOptionsBuilder<T>>, IEntityOptionsBuilder
    {
        private readonly Dictionary<MemberInfo, IPropertyOptionsBuilder> _properties = new Dictionary<MemberInfo, IPropertyOptionsBuilder>();
        private readonly Type _type;

        private bool _defaultForIsUnicode = false;
        private int _defaultForNumberOfDecimals = 4;

        bool IEntityOptionsBuilder.DefaultForIsUnicode => _defaultForIsUnicode;
        int IEntityOptionsBuilder.DefaultForNumberOfDecimals => _defaultForNumberOfDecimals;
        IPropertyOptionsBuilder? IEntityOptionsBuilder.GetPropertyOptions(MemberInfo memberInfo) => GetPropertyOptions(memberInfo);

        internal EntityOptionsBuilder()
        {
            _type = typeof(T);
        }

        internal IPropertyOptionsBuilder? GetPropertyOptions(MemberInfo memberInfo)
        {
            return _properties.TryGetValue(memberInfo, out IPropertyOptionsBuilder? propertyOptions) ? propertyOptions : null;
        }

        /// <summary>
        /// Returns an object that can be used to configure a property of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured (e.g. p => p.Id).</param>
        /// <returns>An object that can be used to configure the property.</returns>
        public PropertyOptionsBuilder<TProperty> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            var property = (MemberExpression)propertyExpression.Body;

            if (!_properties.TryGetValue(property.Member, out IPropertyOptionsBuilder? propertyOptions))
            {
                propertyOptions = new PropertyOptionsBuilder<TProperty>(property.Member);
                _properties.Add(property.Member, propertyOptions);
            }

            return (PropertyOptionsBuilder<TProperty>)propertyOptions;
        }

        /// <summary>
        /// Sets the default behavior for the handling of unicode characters in <see cref="string"/> properties.
        /// </summary>
        /// <param name="isUnicode">A value indicating whether support for unicode characters is enabled by default.</param>
        /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
        public EntityOptionsBuilder<T> DefaultForIsUnicode(bool isUnicode)
        {
            _defaultForIsUnicode = isUnicode;
            return this;
        }

        /// <summary>
        /// Sets the default number of decimals supported for <see cref="decimal"/> properties.
        /// </summary>
        /// <param name="numberOfDecimals">A value indicating the number of decimals supported by default.</param>
        /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
        /// <exception cref="ArgumentException"></exception>
        public EntityOptionsBuilder<T> DefaultForNumberOfDecimals(int numberOfDecimals)
        {
            Validations.ValidateNumberOfDecimals(numberOfDecimals);
            _defaultForNumberOfDecimals = numberOfDecimals;
            return this;
        }

#pragma warning disable CS1591
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_type);
            hash.Add(_defaultForIsUnicode);
            hash.Add(_defaultForNumberOfDecimals);

            foreach (var value in _properties.Values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }

        public bool Equals(EntityOptionsBuilder<T>? other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _type == other._type &&
                _defaultForIsUnicode == other._defaultForIsUnicode &&
                _defaultForNumberOfDecimals == other._defaultForNumberOfDecimals &&
                _properties.Values.SequenceEqual(other._properties.Values);
        }

        public override bool Equals(object? obj) => Equals(obj as EntityOptionsBuilder<T>);
#pragma warning restore CS1591
    }

    internal interface IEntityOptionsBuilder
    {
        bool DefaultForIsUnicode { get; }
        int DefaultForNumberOfDecimals { get; }
        IPropertyOptionsBuilder? GetPropertyOptions(MemberInfo memberInfo);
    }
}
