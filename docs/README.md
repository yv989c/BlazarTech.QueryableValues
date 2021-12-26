# QueryableValues

This library allows us to efficiently compose an [IEnumerable<T>] in our [Entity Framework Core] queries when using the [SQL Server Database Provider]. This is done by using the `AsQueryableValues` extension method that is made available on the [DbContext] class. Everything is evaluated on the server with a single roundtrip, in a way that preserves the query's [execution plan], even when the values behind the [IEnumerable<T>] are changed on subsequent executions.

The supported types for `T` are:
- Simple Type: [Int32], [Int64], [Decimal], [Double], [DateTime], [DateTimeOffset], [Guid], and [String].
- Complex Type:
  - Can be an anonymous type.
  - Can be a user defined class or struct, with read/write properties and a public constructor.
  - Must have one or more simple type properties.

For a detailed explanation, please continue reading [here][readme-background].

## When Should I Use It?
The `AsQueryableValues` extension method is intended for queries that are dependent on a *non-constant* sequence of external values. In this case, the underline SQL query will be efficient on subsequent executions.

It provides a solution to the following long standing EF Core [issue](https://github.com/dotnet/efcore/issues/13617) and enables other currently unsupported scenarios; like the ability to efficiently create joins with in-memory data.

## Getting Started

### Installation
QueryableValues is distributed as a [NuGet Package]. The major version number of this library is aligned with the version of [Entity Framework Core] that's supported by it; for example, if you are using EF Core 5, then you must use version 5 of QueryableValues.

Please choose the appropriate command below to install it using the NuGet Package Manager Console window in Visual Studio:

EF Core | Command
:---: | ---
3.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 3.2.0`
5.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 5.2.0`
6.x | `Install-Package BlazarTech.QueryableValues.SqlServer -Version 6.2.0`

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
The `AsQueryableValues` extension method is provided by the `BlazarTech.QueryableValues` namespace, therefore, you must add the following `using` directive to your source code file in order for it to appear as a method of your [DbContext] instance:
```
using BlazarTech.QueryableValues;
```

Below you can find a few examples composing a query using the values provided by an [IEnumerable<T>].

#### Simple Type Examples
Using the [Contains][ContainsQueryable] LINQ method:

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
Using the [Join] LINQ method:
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
#### Complex Type Examples
```c#
// If your IEnumerable<T> variable's item type is a complex type with many properties,
// project only what you need to a new variable and use it in your query.
var projectedItems = items.Select(i => new { i.CategoryId, i.ColorName });

var myQuery = 
    from p in dbContext.Product
    join pi in dbContext.AsQueryableValues(projectedItems) on new { p.CategoryId, p.ColorName } equals new { pi.CategoryId, pi.ColorName }
    select new
    {
        p.ProductId,
        p.Description
    };
```
**About Complex Types**
> :warning: All the data provided by this type is transmitted to the server, therefore, ensure that it only contains the properties that you need for your query. Not following this recommendation will degrade the query's performance.

> :warning: There is a limit of up to ten properties for any given simple type (e.g., cannot have more than ten [Int32] properties). Exceeding that limit will cause an exception and may also be a sign that you should rethink your strategy.

## Do You Want to Know More? 📚
Please take a look at the repository [here](https://github.com/yv989c/BlazarTech.QueryableValues).


[Entity Framework Core]: https://docs.microsoft.com/en-us/ef/core/
[SQL Server Database Provider]: https://docs.microsoft.com/en-us/ef/core/providers/sql-server/
[DbContext]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext
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
[execution plan]: https://docs.microsoft.com/en-us/sql/relational-databases/query-processing-architecture-guide?#execution-plan-caching-and-reuse
[Int32]: https://docs.microsoft.com/en-us/dotnet/api/system.int32
[Int64]: https://docs.microsoft.com/en-us/dotnet/api/system.int64
[Decimal]: https://docs.microsoft.com/en-us/dotnet/api/system.decimal
[Double]: https://docs.microsoft.com/en-us/dotnet/api/system.double
[DateTime]: https://docs.microsoft.com/en-us/dotnet/api/system.datetime
[DateTimeOffset]: https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset
[Guid]: https://docs.microsoft.com/en-us/dotnet/api/system.guid
[String]: https://docs.microsoft.com/en-us/dotnet/api/system.string
