using BlazarTech.QueryableValues.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazarTech.QueryableValues
{
    internal interface IQueryableFactory
    {
        IQueryable<byte> Create(DbContext dbContext, IEnumerable<byte> values);
        IQueryable<short> Create(DbContext dbContext, IEnumerable<short> values);
        IQueryable<int> Create(DbContext dbContext, IEnumerable<int> values);
        IQueryable<long> Create(DbContext dbContext, IEnumerable<long> values);
        IQueryable<decimal> Create(DbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals);
        IQueryable<float> Create(DbContext dbContext, IEnumerable<float> values);
        IQueryable<double> Create(DbContext dbContext, IEnumerable<double> values);
        IQueryable<DateTime> Create(DbContext dbContext, IEnumerable<DateTime> values);
        IQueryable<DateTimeOffset> Create(DbContext dbContext, IEnumerable<DateTimeOffset> values);
        IQueryable<char> Create(DbContext dbContext, IEnumerable<char> values, bool isUnicode);
        IQueryable<string> Create(DbContext dbContext, IEnumerable<string> values, bool isUnicode);
        IQueryable<Guid> Create(DbContext dbContext, IEnumerable<Guid> values);
        public IQueryable<TEnum> Create<TEnum>(DbContext dbContext, IEnumerable<TEnum> values)
            where TEnum : struct, Enum;
        IQueryable<TSource> Create<TSource>(DbContext dbContext, IEnumerable<TSource> values, Action<EntityOptionsBuilder<TSource>>? configure)
            where TSource : notnull;

#if EFCORE8
        IQueryable<DateOnly> Create(DbContext dbContext, IEnumerable<DateOnly> values);
        IQueryable<TimeOnly> Create(DbContext dbContext, IEnumerable<TimeOnly> values);
#endif
    }
}
