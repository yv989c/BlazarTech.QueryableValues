#if EFCORE
using Microsoft.EntityFrameworkCore;

namespace BlazarTech.QueryableValues
{
    /// <summary>
    /// Makes the <see cref="QueryableValuesDbContextExtensions"/> available on the type implementing this interface.
    /// </summary>
    /// <remarks>
    /// Useful when your <see cref="DbContext"/> is behind an interface.
    /// You can implement <see cref="IQueryableValuesEnabledDbContext"/> on that interface to make the <see cref="QueryableValuesDbContextExtensions"/> available on it.
    /// </remarks>
    public interface IQueryableValuesEnabledDbContext
    {
    }
}
#endif