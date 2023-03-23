using System;
using System.Collections.Generic;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// QueryableValues options for SQL Server.
    /// </summary>
    public sealed class QueryableValuesSqlServerOptions
    {
        internal bool WithUseSelectTopOptimization { get; private set; } = true;
        internal bool WithUseDeferredEnumeration { get; private set; } = true;
        internal SerializationOptions WithSerializationOptions { get; private set; } = SerializationOptions.Auto;

        /// <summary>
        /// When possible, uses a <c>TOP(n)</c> clause in the underlying <c>SELECT</c> statement to assist SQL Server memory grant estimation. The default is <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// No-op on EF Core 3.
        /// See <see href="https://techcommunity.microsoft.com/t5/sql-server-blog/understanding-sql-server-memory-grant/ba-p/383595">Understanding SQL server memory grant</see> for more information.
        /// </remarks>
        /// <param name="useSelectTopOptimization"></param>
        /// <returns>The same <see cref="QueryableValuesSqlServerOptions"/> instance so subsequent configurations can be chained.</returns>
        public QueryableValuesSqlServerOptions UseSelectTopOptimization(bool useSelectTopOptimization = true)
        {
            WithUseSelectTopOptimization = useSelectTopOptimization;
            return this;
        }

        /// <summary>
        /// Configures serialization options. The default is <see cref="SerializationOptions.Auto"/>.
        /// </summary>
        /// <param name="options">Serialization options.</param>
        /// <returns>The same <see cref="QueryableValuesSqlServerOptions"/> instance so subsequent configurations can be chained.</returns>
        public QueryableValuesSqlServerOptions Serialization(SerializationOptions options = SerializationOptions.Auto)
        {
            if (Enum.IsDefined(typeof(SerializationOptions), options))
            {
                WithSerializationOptions = options;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            return this;
        }

#if !EFCORE3
        /// <summary>
        /// If <see langword="true"/>, the <see cref="IEnumerable{T}"/> provided to any of the <c>AsQueryableValues</c> methods will be enumerated when the query is materialized; otherwise, it will be immediately enumerated once. The default is <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Leaving this feature enabled has the following advantages:<br/>
        /// - If your sequence of values is behind an <see cref="IEnumerable{T}"/>, it will be enumerated only when needed.<br/>
        /// - The sequence is enumerated every time the query is materialized, allowing the query to be aware of any changes done to the underlying sequence.
        /// </para>
        /// <para>
        /// You may want to disable this feature if you rely on the <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToQueryString(System.Linq.IQueryable)"/> method across your application.
        /// As of EF 7.0, the implementation of that API is making incompatible assumptions about the underlying ADO.NET query parameters, resulting in an <see cref="InvalidCastException"/> when this option is enabled.
        /// </para>
        /// </remarks>
        /// <param name="useDeferredEnumeration"></param>
        /// <returns>The same <see cref="QueryableValuesSqlServerOptions"/> instance so subsequent configurations can be chained.</returns>
        public QueryableValuesSqlServerOptions UseDeferredEnumeration(bool useDeferredEnumeration = true)
        {
            WithUseDeferredEnumeration = useDeferredEnumeration;
            return this;
        }
#endif
    }
}
