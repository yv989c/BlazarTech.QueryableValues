using BlazarTech.QueryableValues.Serializers;
using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal sealed class DeferredValues<T, T2> : IDeferredValues
        where T : notnull
        where T2 : notnull
    {
        private readonly ISerializer _serializer;
        private readonly ValuesWrapper<T, T2> _valuesWrapper;

        public IReadOnlyList<EntityPropertyMapping> Mappings { get; }

        public bool HasCount
        {
            get
            {
                return _valuesWrapper.OriginalValues.TryGetNonEnumeratedCount(out _);
            }
        }

        public DeferredValues(ISerializer serializer, ValuesWrapper<T, T2> valuesWrapper)
        {
            _serializer = serializer;
            _valuesWrapper = valuesWrapper;
            Mappings = EntityPropertyMapping.GetMappings<T2>();
        }

        public string ToString(IFormatProvider? provider) => _serializer.Serialize(_valuesWrapper.ProjectedValues, Mappings);

        public TypeCode GetTypeCode() => throw new NotImplementedException();
        public bool ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
        public byte ToByte(IFormatProvider? provider) => throw new NotImplementedException();
        public char ToChar(IFormatProvider? provider) => throw new NotImplementedException();
        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
        public decimal ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
        public double ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
        public short ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public int ToInt32(IFormatProvider? provider) => throw new NotImplementedException();

        public long ToInt64(IFormatProvider? provider)
        {
            if (_valuesWrapper.OriginalValues.TryGetNonEnumeratedCount(out int count))
            {
                return count;
            }
            else
            {
                // Count not available. How did this happen?
                return int.MaxValue;
            }
        }

        public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
        public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotImplementedException();
        public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
        public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();
    }

    internal interface IDeferredValues : IConvertible
    {
        IReadOnlyList<EntityPropertyMapping> Mappings { get; }
        bool HasCount { get; }
    }
}
