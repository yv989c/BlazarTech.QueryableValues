using BlazarTech.QueryableValues.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Extension methods provided by QueryableValues on the <see cref="IQueryableValuesEnabledDbContext"/> interface.
    /// </summary>
    public static class QueryableValuesEnabledDbContextExtensions
    {
        private static DbContext GetDbContext(IQueryableValuesEnabledDbContext dbContext)
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (dbContext is DbContext castedDbContext)
            {
                return castedDbContext;
            }
            else
            {
                throw new InvalidOperationException("QueryableValues only works on a Microsoft.EntityFrameworkCore.DbContext type.");
            }
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{byte})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{byte})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{byte})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{byte})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{byte})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<byte> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<byte> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{short})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{short})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{short})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{short})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{short})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<short> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<short> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{int})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{int})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{int})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{int})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{int})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<int> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<int> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{long})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{long})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{long})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{long})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{long})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<long> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<long> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)" path="/param[@name='values']"/></param>
        /// <param name="numberOfDecimals"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)" path="/param[@name='numberOfDecimals']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{decimal}, int)"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<decimal> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<decimal> values, int numberOfDecimals = 4)
        {
            return GetDbContext(dbContext).AsQueryableValues(values, numberOfDecimals);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{float})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{float})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{float})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{float})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{float})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<float> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<float> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{double})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{double})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{double})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{double})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{double})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<double> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<double> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTime})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTime})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTime})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTime})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTime})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<DateTime> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<DateTime> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTimeOffset})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTimeOffset})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTimeOffset})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTimeOffset})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{DateTimeOffset})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<DateTimeOffset> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<DateTimeOffset> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)" path="/param[@name='values']"/></param>
        /// <param name="isUnicode"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)" path="/param[@name='isUnicode']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{char}, bool)"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<char> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<char> values, bool isUnicode = false)
        {
            return GetDbContext(dbContext).AsQueryableValues(values, isUnicode: isUnicode);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)" path="/param[@name='values']"/></param>
        /// <param name="isUnicode"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)" path="/param[@name='isUnicode']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{string}, bool)"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<string> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<string> values, bool isUnicode = false)
        {
            return GetDbContext(dbContext).AsQueryableValues(values, isUnicode: isUnicode);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{Guid})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{Guid})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{Guid})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{Guid})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues(DbContext, IEnumerable{Guid})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<Guid> AsQueryableValues(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<Guid> values)
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TEnum}(DbContext, IEnumerable{TEnum})"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TEnum}(DbContext, IEnumerable{TEnum})" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TEnum}(DbContext, IEnumerable{TEnum})" path="/param[@name='values']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TEnum}(DbContext, IEnumerable{TEnum})"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TEnum}(DbContext, IEnumerable{TEnum})"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<TEnum> AsQueryableValues<TEnum>(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<TEnum> values)
            where TEnum : struct, Enum
        {
            return GetDbContext(dbContext).AsQueryableValues(values);
        }

        /// <summary>
        /// <inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)"/>
        /// </summary>
        /// <param name="dbContext"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)" path="/param[@name='dbContext']"/></param>
        /// <param name="values"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)" path="/param[@name='values']"/></param>
        /// <param name="configure"><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)" path="/param[@name='configure']"/></param>
        /// <returns><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)"/></returns>
        /// <remarks><inheritdoc cref="QueryableValuesDbContextExtensions.AsQueryableValues{TSource}(DbContext, IEnumerable{TSource}, Action{EntityOptionsBuilder{TSource}}?)"/></remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IQueryable<TSource> AsQueryableValues<TSource>(this IQueryableValuesEnabledDbContext dbContext, IEnumerable<TSource> values, Action<EntityOptionsBuilder<TSource>>? configure = null)
            where TSource : notnull
        {
            return GetDbContext(dbContext).AsQueryableValues(values, configure);
        }
    }
}
