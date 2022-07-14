namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// QueryableValues options for SQL Server.
    /// </summary>
    public sealed class QueryableValuesSqlServerOptions
    {
        internal bool WithUseSelectTopOptimization { get; private set; } = true;

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
    }
}
