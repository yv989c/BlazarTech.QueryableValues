using BlazarTech.QueryableValues.Serializers;
using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal abstract class DeferredValues<T> : IConvertible
        where T : notnull
    {
        protected readonly ISerializer _serializer;
        protected readonly IEnumerable<T> _values;

        // todo: Expose API to turn this behavior off by the user.
        /// <summary>
        /// Used to optimize the generated SQL by providing a TOP(n) on the SELECT statement.
        /// In my tests, I observed improved memory grant estimation by SQL Server's query engine.
        /// </summary>
        public bool HasCount
        {
            get
            {
#if EFCORE3
                // In my EF Core 3 tests, it seems that on the first execution of the query,
                // it is caching the values from the parameters provided to the FromSqlRaw method.
                // This imposes a problem when trying to optimize the SQL using the HasCount property in this class.
                // It is critical to know the exact number of elements behind "values" at execution time,
                // this is because the number of items behind "values" can change between executions of the query,
                // therefore, this optimization cannot be done in a reliable way under EF Core 3.
                //
                // Under EF Core 5 and 6 this is not an issue. The parameters are always evaluated on each execution.
                return false;
#else
                return _values.TryGetNonEnumeratedCount(out _);
#endif
            }
        }

        public DeferredValues(ISerializer serializer, IEnumerable<T> values)
        {
            _serializer = serializer;
            _values = values;
        }

        public abstract string ToString(IFormatProvider? provider);

        public TypeCode GetTypeCode() => throw new NotImplementedException();
        public bool ToBoolean(IFormatProvider? provider) => throw new NotImplementedException();
        public byte ToByte(IFormatProvider? provider) => throw new NotImplementedException();
        public char ToChar(IFormatProvider? provider) => throw new NotImplementedException();
        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotImplementedException();
        public decimal ToDecimal(IFormatProvider? provider) => throw new NotImplementedException();
        public double ToDouble(IFormatProvider? provider) => throw new NotImplementedException();
        public short ToInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public int ToInt32(IFormatProvider? provider) => throw new NotImplementedException();

#if EFCORE3
        public long ToInt64(IFormatProvider? provider) => throw new NotImplementedException();
#else
        public long ToInt64(IFormatProvider? provider)
        {
            if (_values.TryGetNonEnumeratedCount(out int count))
            {
                return count;
            }
            else
            {
                throw new InvalidOperationException("Count not available. (how did this happen?)");
            }
        }
#endif

        public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
        public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotImplementedException();
        public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
        public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();
    }

    internal sealed class DeferredByteValues : DeferredValues<byte>
    {
        public DeferredByteValues(ISerializer serializer, IEnumerable<byte> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredInt16Values : DeferredValues<short>
    {
        public DeferredInt16Values(ISerializer serializer, IEnumerable<short> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredInt32Values : DeferredValues<int>
    {
        public DeferredInt32Values(ISerializer serializer, IEnumerable<int> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredInt64Values : DeferredValues<long>
    {
        public DeferredInt64Values(ISerializer serializer, IEnumerable<long> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredDecimalValues : DeferredValues<decimal>
    {
        public DeferredDecimalValues(ISerializer serializer, IEnumerable<decimal> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredSingleValues : DeferredValues<float>
    {
        public DeferredSingleValues(ISerializer serializer, IEnumerable<float> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredDoubleValues : DeferredValues<double>
    {
        public DeferredDoubleValues(ISerializer serializer, IEnumerable<double> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredDateTimeValues : DeferredValues<DateTime>
    {
        public DeferredDateTimeValues(ISerializer serializer, IEnumerable<DateTime> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredDateTimeOffsetValues : DeferredValues<DateTimeOffset>
    {
        public DeferredDateTimeOffsetValues(ISerializer serializer, IEnumerable<DateTimeOffset> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredGuidValues : DeferredValues<Guid>
    {
        public DeferredGuidValues(ISerializer serializer, IEnumerable<Guid> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredCharValues : DeferredValues<char>
    {
        public DeferredCharValues(ISerializer serializer, IEnumerable<char> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredStringValues : DeferredValues<string>
    {
        public DeferredStringValues(ISerializer serializer, IEnumerable<string> values) : base(serializer, values) { }
        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values);
    }

    internal sealed class DeferredEntityValues<T> : DeferredValues<T>
        where T : notnull
    {
        private readonly IReadOnlyList<EntityPropertyMapping> _mappings;

        public DeferredEntityValues(ISerializer serializer, IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> mappings)
            : base(serializer, values)
        {
            _mappings = mappings;
        }

        public override string ToString(IFormatProvider? provider) => _serializer.Serialize(_values, _mappings);
    }
}
