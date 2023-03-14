# QueryableValues

[![MIT License](https://badgen.net/badge/license/MIT/blue)](https://github.com/yv989c/BlazarTech.QueryableValues/blob/main/LICENSE.md)
[![GitHub Stars](https://badgen.net/github/stars/yv989c/BlazarTech.QueryableValues?icon=github)][Repository]
[![Nuget Downloads](https://badgen.net/nuget/dt/BlazarTech.QueryableValues.SqlServer?icon=nuget)][NuGet Package]

This library allows you to efficiently compose an [IEnumerable&lt;T&gt;] in your [Entity Framework Core] queries when using the [SQL Server Database Provider]. This is accomplished by using the `AsQueryableValues` extension method available on the [DbContext] class. Everything is evaluated on the server with a single round trip, in a way that preserves the query's [execution plan], even when the values behind the [IEnumerable&lt;T&gt;] are changed on subsequent executions.

The supported types for `T` are:
- Simple Type: [Byte], [Int16], [Int32], [Int64], [Decimal], [Single], [Double], [DateTime], [DateTimeOffset], [Guid], [Char], and [String].
- Complex Type:
  - Can be an anonymous type.
  - Can be a user-defined class or struct with read/write properties and a public constructor.
  - Must have one or more simple type properties, including [Boolean].

For a detailed explanation of the problem solved by QueryableValues, please continue reading [here][readme-background].

> 💡 Still on Entity Framework 6 (non-core)? Then [QueryableValues `EF6 Edition`](https://github.com/yv989c/BlazarTech.QueryableValues.EF6) is what you need.

## When Should You Use It?
The `AsQueryableValues` extension method is intended for queries that are dependent upon a *non-constant* sequence of external values. In such cases, the underlying SQL query will be efficient on subsequent executions.

It provides a solution to the following long standing [EF Core issue](https://github.com/dotnet/efcore/issues/13617) and enables other currently unsupported scenarios; like the ability to efficiently create joins with in-memory data.

## Your Support is Appreciated!
If you feel that this solution has provided you some value, please consider [buying me a ☕][BuyMeACoffee].

[![Buy me a coffee][BuyMeACoffeeButton]][BuyMeACoffee]

Your ⭐ on [this repository][Repository] also helps! Thanks! 🖖🙂

# Getting Started

## Installation
QueryableValues is distributed as a [NuGet Package]. The major version number of this library is aligned with the version of [Entity Framework Core] by which it's supported (e.g. If you are using EF Core 5, then you must use version 5 of QueryableValues).

## Configuration
Look for the place in your code where you are setting up your [DbContext] and calling the [UseSqlServer] extension method, then use a lambda expression to access the `SqlServerDbContextOptionsBuilder` provided by it. It is on this builder that you must call the `UseQueryableValues` extension method as shown in the following simplified examples:

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

## How Do You Use It?
The `AsQueryableValues` extension method is provided by the `BlazarTech.QueryableValues` namespace; therefore, you must add the following `using` directive to your source code file for it to appear as a method of your [DbContext] instance:
```c#
using BlazarTech.QueryableValues;
```

> 💡 If you access your [DbContext] via an interface, you can also make the `AsQueryableValues` extension methods available on it by inheriting from the `IQueryableValuesEnabledDbContext` interface.

Below are a few examples composing a query using the values provided by an [IEnumerable&lt;T&gt;].

### Simple Type Examples
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
    };
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
    };
```
### Complex Type Example
```c#
// Performance Tip:
// If your IEnumerable<T> item type (T) has many properties, project only 
// the ones you need to a new variable and use it in your query.
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
> :warning: All the data provided by this type is transmitted to the server; therefore, ensure that it only contains the properties you need for your query. Not following this recommendation will degrade the query's performance.

> :warning: There is a limit of up to 10 properties for any given simple type (e.g. cannot have more than 10 [Int32] properties). Exceeding that limit will cause an exception and may also suggest that you should rethink your strategy.

## Do You Want To Know More? 📚
Please take a look at the [repository][Repository].


[Entity Framework Core]: https://docs.microsoft.com/en-us/ef/core/
[SQL Server Database Provider]: https://docs.microsoft.com/en-us/ef/core/providers/sql-server/
[DbContext]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext
[ContainsEnumerable]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.contains
[ContainsQueryable]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.queryable.contains
[Join]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.queryable.join
[UseSqlServer]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.usesqlserver
[sp_executesql]: https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-executesql-transact-sql
[SqlCommand]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcommand
[IEnumerable&lt;T&gt;]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable
[IQueryable&lt;T&gt;]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1
[NuGet Package]: https://www.nuget.org/packages/BlazarTech.QueryableValues.SqlServer/
[readme-background]: https://github.com/yv989c/BlazarTech.QueryableValues#background-
[execution plan]: https://docs.microsoft.com/en-us/sql/relational-databases/query-processing-architecture-guide?#execution-plan-caching-and-reuse
[Boolean]: https://docs.microsoft.com/en-us/dotnet/api/system.boolean
[Byte]: https://docs.microsoft.com/en-us/dotnet/api/system.byte
[Int16]: https://docs.microsoft.com/en-us/dotnet/api/system.int16
[Int32]: https://docs.microsoft.com/en-us/dotnet/api/system.int32
[Int64]: https://docs.microsoft.com/en-us/dotnet/api/system.int64
[Decimal]: https://docs.microsoft.com/en-us/dotnet/api/system.decimal
[Single]: https://docs.microsoft.com/en-us/dotnet/api/system.single
[Double]: https://docs.microsoft.com/en-us/dotnet/api/system.double
[DateTime]: https://docs.microsoft.com/en-us/dotnet/api/system.datetime
[DateTimeOffset]: https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset
[Guid]: https://docs.microsoft.com/en-us/dotnet/api/system.guid
[Char]: https://docs.microsoft.com/en-us/dotnet/api/system.char
[String]: https://docs.microsoft.com/en-us/dotnet/api/system.string
[BuyMeACoffee]: https://www.buymeacoffee.com/yv989c
[BuyMeACoffeeButton]: https://raw.githubusercontent.com/yv989c/BlazarTech.QueryableValues/develop/docs/images/bmc-48.svg
[Repository]: https://github.com/yv989c/BlazarTech.QueryableValues