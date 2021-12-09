# QueryableValues [![CI/CD Workflow](https://github.com/yv989c/BlazarTech.QueryableValues/actions/workflows/ci-workflow.yml/badge.svg)](https://github.com/yv989c/BlazarTech.QueryableValues/actions/workflows/ci-workflow.yml)
This library improves the efficiency of some types of queries in [Entity Framework Core] when using the [SQL Server Database Provider].

The provided `AsQueryableValues` extension method on the [DbContext] class allows us to efficiently compose an [IEnumerable<T>] in our queries when it consist of *non-constant* values. It returns an [IQueryable<T>] that in the context of a query can be treated as any other entity in our [DbContext]. Everything is evaluated on the server.

The supported types for `T` are: `Int32`, `Int64`, `Decimal`, `Double`, `DateTime`, `DateTimeOffset`, `Guid`, and `String`.

For a detailed explanation, please continue reading [here][readme-background].

## When Should I Use It?
The `AsQueryableValues` extension method is intended for queries that are dependent on a *non-constant* sequence of external values. In this case, the underline SQL query will be efficient on subsequent executions.

## Getting Started

### Installation
QueryableValues is distributed as a [NuGet Package]. The major version number of this library is aligned with the version of [Entity Framework Core] that's supported by it; for example, if you are using EF Core 5, then you must use version 5 of QueryableValues.

Please choose the appropriate command below to install it using the NuGet Package Manager Console window in Visual Studio:

EF Core | Command
:---: | ---
3.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 3.1.0`
5.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 5.1.0`
6.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 6.1.0`

### Configuration
Look for the place in your code where you are setting up your [DbContext] and calling the [UseSqlServer] extension method, then use a lambda expression to access the `SqlServerDbContextOptionsBuilder` provided by it. It is on this builder that you must call the `UseQueryableValues` extension method, as shown in the following simplified examples:

When using the `OnConfiguring` method inside your [DbContext]:
```c#
using BlazarTech.QueryableValues;

public class MyDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "MyConnectionString",
            sqlServerOptionsBuilder =>
            {
                sqlServerOptionsBuilder.UseQueryableValues();
            }
        );
    }
}
```
When setting up the [DbContext] at registration time using dependency injection:
```c#
using BlazarTech.QueryableValues;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<MyDbContext>(optionsBuilder => {
            optionsBuilder.UseSqlServer(
                "MyConnectionString",
                sqlServerOptionsBuilder =>
                {
                    sqlServerOptionsBuilder.UseQueryableValues();
                }
            );
        });
    }
}
```

### How Do I Use It?
The `AsQueryableValues` extension method is provided by the `BlazarTech.QueryableValues` namespace, therefore, you must add the following `using` directive to your source code file in order for it to appear as a method of your [DbContext] instance.
```
using BlazarTech.QueryableValues;
```

Below are two patterns that you can use to retrieve data from an entity based on values provided by an [IEnumerable<T>].

Using the [Contains][ContainsQueryable] LINQ method

```c#
// Sample values.
IEnumerable<int> values = Enumerable.Range(1, 10);

// Example #1 (LINQ method syntax)
var myQuery1 = dbContext.MyEntities
    .Where(i => dbContext
        .AsQueryableValues(values)
        .Contains(i.MyEntityID)
    )
    .Select(i => new
    {
        i.MyEntityID,
        i.PropA
    });

// Example #2 (LINQ query syntax)
var myQuery2 = 
    from i in dbContext.MyEntities
    where dbContext
        .AsQueryableValues(values)
        .Contains(i.MyEntityID)
    select new
    {
        i.MyEntityID,
        i.PropA
    });
```
Using the [Join] LINQ method
```c#
// Sample values.
IEnumerable<int> values = Enumerable.Range(1, 10);

// Example #1 (LINQ method syntax)
var myQuery1 = dbContext.MyEntities
    .Join(
        dbContext.AsQueryableValues(values),
        i => i.MyEntityID,
        v => v,
        (i, v) => new
        {
            i.MyEntityID,
            i.PropA
        }
    );

// Example #2 (LINQ query syntax)
var myQuery2 = 
    from i in dbContext.MyEntities
    join v in dbContext.AsQueryableValues(values) on i.MyEntityID equals v 
    select new
    {
        i.MyEntityID,
        i.PropA
    });
```

## Do You Want to Know More? 📚
Please take a look at the repository [here](https://github.com/yv989c/BlazarTech.QueryableValues).


[Entity Framework Core]: https://docs.microsoft.com/en-us/ef/core/
[SQL Server Database Provider]: https://docs.microsoft.com/en-us/ef/core/providers/sql-server/
[DbContext]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext?view=efcore-5.0
[ContainsEnumerable]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.contains
[ContainsQueryable]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.queryable.contains
[Join]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.queryable.join
[UseSqlServer]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.usesqlserver
[sp_executesql]: https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-executesql-transact-sql
[SqlCommand]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcommand
[IEnumerable<T>]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable
[IQueryable<T>]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1
[NuGet Package]: https://www.nuget.org/packages/BlazarTech.QueryableValues.SqlServer/
[readme-background]: https://github.com/yv989c/BlazarTech.QueryableValues#background-
