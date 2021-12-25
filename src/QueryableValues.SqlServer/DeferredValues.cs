using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    internal abstract class DeferredValues<T> : IConvertible
        where T : notnull
    {
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

        public DeferredValues(IEnumerable<T> values)
        {
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

#if EFCORE3
        public int ToInt32(IFormatProvider? provider) => throw new NotImplementedException();
#else
        public int ToInt32(IFormatProvider? provider)
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
        public long ToInt64(IFormatProvider? provider) => throw new NotImplementedException();
        public sbyte ToSByte(IFormatProvider? provider) => throw new NotImplementedException();
        public float ToSingle(IFormatProvider? provider) => throw new NotImplementedException();
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotImplementedException();
        public ushort ToUInt16(IFormatProvider? provider) => throw new NotImplementedException();
        public uint ToUInt32(IFormatProvider? provider) => throw new NotImplementedException();
        public ulong ToUInt64(IFormatProvider? provider) => throw new NotImplementedException();
    }

    internal sealed class DeferredInt32Values : DeferredValues<int>
    {
        public DeferredInt32Values(IEnumerable<int> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredInt64Values : DeferredValues<long>
    {
        public DeferredInt64Values(IEnumerable<long> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredDecimalValues : DeferredValues<decimal>
    {
        public DeferredDecimalValues(IEnumerable<decimal> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredDoubleValues : DeferredValues<double>
    {
        public DeferredDoubleValues(IEnumerable<double> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredDateTimeValues : DeferredValues<DateTime>
    {
        public DeferredDateTimeValues(IEnumerable<DateTime> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredDateTimeOffsetValues : DeferredValues<DateTimeOffset>
    {
        public DeferredDateTimeOffsetValues(IEnumerable<DateTimeOffset> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredGuidValues : DeferredValues<Guid>
    {
        public DeferredGuidValues(IEnumerable<Guid> values) : base(values) { }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredStringValues : DeferredValues<string>
    {
        public DeferredStringValues(IEnumerable<string> values) : base(values) { }

        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values);
    }

    internal sealed class DeferredEntityValues<T> : DeferredValues<T>
        where T : notnull
    {
        private readonly IReadOnlyList<EntityPropertyMapping> _mappings;

        public DeferredEntityValues(IEnumerable<T> values, IReadOnlyList<EntityPropertyMapping> mappings)
            : base(values)
        {
            _mappings = mappings;
        }
        public override string ToString(IFormatProvider? provider) => XmlUtil.GetXml(_values, _mappings);
    }
}
