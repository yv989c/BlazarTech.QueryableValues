#if EFCORE
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace BlazarTech.QueryableValues.SqlServer
{
    internal sealed class ExtensionOptions
    {
        public QueryableValuesSqlServerOptions Options { get; }

        public ExtensionOptions(IDbContextOptions dbContextOptions)
        {
            Options = (dbContextOptions.FindExtension<QueryableValuesSqlServerExtension>()?.Options) ?? throw new InvalidOperationException();
        }
    }
}
#endif
