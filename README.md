<p align="center">
    <img src="/docs/images/icon.png" alt="Logo" style="width: 80px;">
</p>

# QueryableValues

[![MIT License](https://badgen.net/badge/license/MIT/blue)](https://github.com/yv989c/BlazarTech.QueryableValues/blob/main/LICENSE.md)
[![GitHub Stars](https://badgen.net/github/stars/yv989c/BlazarTech.QueryableValues?icon=github)][Repository]
[![Nuget Downloads](https://badgen.net/nuget/dt/BlazarTech.QueryableValues.SqlServer?icon=nuget)][NuGet Package]

> 🤔💭 TLDR; By using QueryableValues, you can incorporate in-memory collections into your EF queries with outstanding performance and flexibility.

This library allows you to efficiently compose an [IEnumerable&lt;T&gt;] in your [Entity Framework Core] queries when using the [SQL Server Database Provider]. This is accomplished by using the `AsQueryableValues` extension method available on the [DbContext] class. Everything is evaluated on the server with a single round trip, in a way that preserves the query's [execution plan], even when the values behind the [IEnumerable&lt;T&gt;] are changed on subsequent executions.

The supported types for `T` are:
- Simple Type: [Byte], [Int16], [Int32], [Int64], [Decimal], [Single], [Double], [DateTime], [DateTimeOffset], [Guid], [Char], and [String].
- Complex Type:
  - Can be an anonymous type.
  - Can be a user-defined class or struct with read/write properties and a public constructor.
  - Must have one or more simple type properties, including [Boolean].

For a detailed explanation of the problem solved by QueryableValues, please continue reading [here][readme-background].

> 💡 QueryableValues boasts over 120 integration tests that are executed on every supported version of EF. These tests ensure reliability and compatibility, giving you added confidence.

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

# Benchmarks
The following [benchmarks] consist of simple EF Core queries that have a dependency on a random sequence of [Int32] and [Guid] values via the `Contains` LINQ method. It shows the performance differences between not using and using QueryableValues. In practice, the benefits of using QueryableValues will be more dramatic on complex EF Core queries and busy environments.

### Benchmarked Libraries
| Package | Version |
| ------- |:-------:|
| Microsoft.EntityFrameworkCore.SqlServer | 6.0.1 |
| BlazarTech.QueryableValues.SqlServer | 6.3.0 |

### BenchmarkDotNet Configuration and System Specs
```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.1466 (20H2/October2020Update)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-GMTUEM : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT

Server=True  InvocationCount=200  IterationCount=25
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=1
```
### SQL Server Instance Specs
```
Microsoft SQL Server 2017 (RTM-GDR) (KB4583456) - 14.0.2037.2 (X64)
Nov  2 2020 19:19:59
Copyright (C) 2017 Microsoft Corporation
Express Edition (64-bit) on Windows 10 Pro 10.0 <X64> (Build 19042: ) (Hypervisor)
```
- The SQL Server instance was running in the same system where the benchmark was executed.
- Shared Memory is the only network protocol that's enabled on this instance.


### Results for Int32

![Benchmarks Int32 Values][BenchmarksInt32]

<details>

| Method  | Values |  Mean (us)   |  Error (us)  |  Std Dev (us)  |  Median (us)  | Ratio | RatioSD | Allocated | 
|---------|--------|--------------|--------------|----------------|---------------|-------|---------|-----------| 
| Without | 2      |  921.20      |  31.30       |  41.78         |  903.80       | 1.00  | 0.00    | 20 KB     | 
| With    | 2      |  734.30      |  45.28       |  60.44         |  696.10       | 0.80  | 0.04    | 51 KB     | 
| Without | 4      |  997.80      |  31.79       |  42.44         |  981.10       | 1.00  | 0.00    | 21 KB     | 
| With    | 4      |  779.40      |  47.22       |  63.04         |  738.70       | 0.78  | 0.05    | 51 KB     | 
| Without | 8      |  1,081.00    |  31.26       |  41.74         |  1,061.30     | 1.00  | 0.00    | 21 KB     | 
| With    | 8      |  814.20      |  47.34       |  63.20         |  775.70       | 0.75  | 0.04    | 51 KB     | 
| Without | 16     |  1,331.70    |  88.81       |  118.56        |  1,283.40     | 1.00  | 0.00    | 23 KB     | 
| With    | 16     |  872.70      |  42.46       |  56.68         |  840.30       | 0.66  | 0.06    | 52 KB     | 
| Without | 32     |  1,731.40    |  40.59       |  54.18         |  1,732.60     | 1.00  | 0.00    | 26 KB     | 
| With    | 32     |  1,006.00    |  47.61       |  63.56         |  973.60       | 0.58  | 0.03    | 53 KB     | 
| Without | 64     |  2,615.40    |  103.77      |  138.53        |  2,540.20     | 1.00  | 0.00    | 31 KB     | 
| With    | 64     |  1,264.20    |  36.95       |  49.33         |  1,239.90     | 0.48  | 0.03    | 55 KB     | 
| Without | 128    |  5,687.30    |  200.05      |  267.06        |  5,588.20     | 1.00  | 0.00    | 41 KB     | 
| With    | 128    |  1,917.00    |  34.06       |  45.47         |  1,897.90     | 0.34  | 0.02    | 60 KB     | 
| Without | 256    |  10,565.00   |  186.05      |  248.37        |  10,473.00    | 1.00  | 0.00    | 63 KB     | 
| With    | 256    |  2,977.00    |  29.38       |  39.23         |  2,964.50     | 0.28  | 0.01    | 69 KB     | 
| Without | 512    |  20,110.50   |  452.28      |  603.79        |  20,108.30    | 1.00  | 0.00    | 106 KB    | 
| With    | 512    |  5,313.10    |  47.66       |  63.62         |  5,340.80     | 0.26  | 0.01    | 88 KB     | 
| Without | 1024   |  46,599.30   |  4,286.13    |  5,721.87      |  48,194.20    | 1.00  | 0.00    | 192 KB    | 
| With    | 1024   |  11,614.40   |  85.81       |  114.55        |  11,619.80    | 0.25  | 0.03    | 128 KB    | 
| Without | 2048   |  105,096.90  |  5,359.60    |  7,154.92      |  106,405.10   | 1.00  | 0.00    | 363 KB    | 
| With    | 2048   |  19,481.40   |  66.66       |  88.99         |  19,474.80    | 0.19  | 0.01    | 213 KB    | 
| Without | 4096   |  177,245.80  |  1,812.40    |  2,419.51      |  176,767.90   | 1.00  | 0.00    | 706 KB    | 
| With    | 4096   |  38,743.00   |  2,422.07    |  3,233.40      |  37,414.70    | 0.22  | 0.02    | 368 KB    | 

</details>

### Results for Guid

![Benchmarks Guid Values][BenchmarksGuid]

<details>

| Method  | Values |  Mean (us)   |  Error (us)  |  Std Dev (us)  |  Median (us)  | Ratio | RatioSD | Allocated | 
|---------|--------|--------------|--------------|----------------|---------------|-------|---------|-----------| 
| Without | 2      |  895.60      |  30.64       |  40.91         |  877.90       | 1.00  | 0.00    | 21 KB     | 
| With    | 2      |  741.80      |  46.44       |  62.00         |  704.40       | 0.83  | 0.04    | 51 KB     | 
| Without | 4      |  968.90      |  33.69       |  44.97         |  950.40       | 1.00  | 0.00    | 22 KB     | 
| With    | 4      |  727.00      |  43.20       |  57.68         |  689.80       | 0.75  | 0.04    | 52 KB     | 
| Without | 8      |  1,075.50    |  34.88       |  46.57         |  1,054.90     | 1.00  | 0.00    | 23 KB     | 
| With    | 8      |  773.10      |  42.45       |  56.67         |  737.10       | 0.72  | 0.04    | 53 KB     | 
| Without | 16     |  1,372.60    |  66.21       |  88.39         |  1,383.80     | 1.00  | 0.00    | 26 KB     | 
| With    | 16     |  808.90      |  40.12       |  53.55         |  777.80       | 0.59  | 0.06    | 55 KB     | 
| Without | 32     |  1,710.70    |  26.25       |  35.04         |  1,699.90     | 1.00  | 0.00    | 33 KB     | 
| With    | 32     |  869.80      |  49.27       |  65.78         |  830.40       | 0.51  | 0.03    | 59 KB     | 
| Without | 64     |  2,656.60    |  30.28       |  40.43         |  2,652.30     | 1.00  | 0.00    | 47 KB     | 
| With    | 64     |  1,038.70    |  58.99       |  78.75         |  994.40       | 0.39  | 0.03    | 67 KB     | 
| Without | 128    |  5,415.90    |  45.76       |  61.09         |  5,417.00     | 1.00  | 0.00    | 74 KB     | 
| With    | 128    |  1,456.30    |  53.76       |  71.77         |  1,424.10     | 0.27  | 0.02    | 84 KB     | 
| Without | 256    |  9,461.50    |  45.09       |  60.20         |  9,469.10     | 1.00  | 0.00    | 128 KB    | 
| With    | 256    |  2,156.00    |  36.01       |  48.07         |  2,139.30     | 0.23  | 0.00    | 120 KB    | 
| Without | 512    |  18,015.10   |  117.47      |  156.82        |  17,946.50    | 1.00  | 0.00    | 219 KB    | 
| With    | 512    |  3,511.30    |  62.41       |  83.32         |  3,460.80     | 0.19  | 0.00    | 197 KB    | 
| Without | 1024   |  44,525.60   |  754.94      |  1,007.82      |  44,601.80    | 1.00  | 0.00    | 419 KB    | 
| With    | 1024   |  7,825.80    |  72.45       |  96.72         |  7,808.20     | 0.18  | 0.00    | 319 KB    | 
| Without | 2048   |  83,843.30   |  778.80      |  1,039.68      |  83,954.70    | 1.00  | 0.00    | 801 KB    | 
| With    | 2048   |  12,372.40   |  207.91      |  277.55        |  12,232.20    | 0.15  | 0.00    | 596 KB    | 
| Without | 4096   |  217,255.80  |  3,458.95    |  4,617.60      |  216,353.20   | 1.00  | 0.00    | 1,566 KB  | 
| With    | 4096   |  24,981.10   |  274.10      |  365.92        |  25,116.70    | 0.12  | 0.00    | 1,132 KB  | 

</details>

---

## Background 📚
When [Entity Framework Core] is set up to use the [SQL Server Database Provider] and it detects the use of variables in a query, in *most cases* it provides its values as parameters to an internal [SqlCommand] object that will execute the translated SQL statement. This is done efficiently by using the [sp_executesql] stored procedure behind the scenes, so if the same SQL statement is executed a second time, the SQL Server instance will likely have a computed [execution plan] in its cache, thereby saving time and system resources.

## The Problem 🤔
We have been in the situation where we need to build a query that must return one or more items based on a sequence of values. The common pattern to do this makes use of the [Contains][ContainsEnumerable] LINQ extension method on the [IEnumerable&lt;T&gt;] interface, then we pass the property of the entity that must match any of the values in the sequence. This way we can retrieve multiple items with a single round trip to the database as shown in the following example:

```c#
var myQuery = dbContext.MyEntities
    .Where(i => listOfValues.Contains(i.MyEntityID))
    .Select(i => new
    {
        i.MyEntityID,
        i.PropB,
        i.PropC
    });
```
The previous query will yield the expected results, but there's a catch. If the sequence of values in our list *is different* on every execution, the underlying SQL query will be built in a way that's not optimal for SQL Server's query engine. Wasting system resources like CPU, memory, IO, and potentially affecting other queries in the instance.

Let's take a look at the following query and the SQL that is generated by the [SQL Server Database Provider] as of version 5.0.11 when the query is materialized:

```c#
var listOfValues = new List<int> { 1, 2, 3 };
var anotherVariable = 100;
var myQuery = dbContext.MyEntities
    .Where(i =>
        listOfValues.Contains(i.MyEntityID) ||
        i.PropB == anotherVariable
    )
    .Select(i => new
    {
        i.MyEntityID,
        i.PropA
    })
    .ToList();
```
**Generated SQL**
```tsql
exec sp_executesql N'SELECT [m].[MyEntityID], [m].[PropA]
FROM [dbo].[MyEntity] AS [m]
WHERE [m].[MyEntityID] IN (1, 2, 3) OR ([m].[PropB] = @__p_1)',N'@__p_1 bigint',@__p_1=100
```
Here we can observe that the values in our list are being hardcoded as part of the SQL statement provided to [sp_executesql] as opposed to them being injected via a parameter, as is the case for our other variable holding the value 100.

Now, let's add another item to the list of values and execute the query again:
```tsql
exec sp_executesql N'SELECT [m].[MyEntityID], [m].[PropA]
FROM [dbo].[MyEntity] AS [m]
WHERE [m].[MyEntityID] IN (1, 2, 3, 4) OR ([m].[PropB] = @__p_1)',N'@__p_1 bigint',@__p_1=100
```
As we can see, a new SQL statement was generated just because we modified the list that's being used in our `Where` predicate. This has the detrimental effect that a previously cached execution plan cannot be reused, forcing SQL Server's query engine to compute a new [execution plan] *every time* it is provided with a SQL statement that it hasn't seen before and increasing the likelihood of flushing other plans in the process.

## Enter AsQueryableValues 🙌
![Parameterize All the Things](/docs/images/parameterize-all-the-things.jpg)

This library provides you with the `AsQueryableValues` extension method made available on the [DbContext] class. It solves the problem explained above by allowing you to build a query that will generate a SQL statement for [sp_executesql] that will remain constant execution after execution, allowing SQL Server to do its best every time by using a previously cached [execution plan]. This will speed up your query on subsequent executions, and conserve system resources.

Let's take a look at the following query making use of this method, which is functionally equivalent to the previous example:
```c#
var myQuery = dbContext.MyEntities
    .Where(i =>
        dbContext.AsQueryableValues(listOfValues).Contains(i.MyEntityID) ||
        i.PropB == anotherVariable
    )
    .Select(i => new
    {
        i.MyEntityID,
        i.PropA
    });
```
**Generated SQL**
```tsql
declare @p3 xml
set @p3=convert(xml,N'<R><V>1</V><V>2</V><V>3</V></R>')
exec sp_executesql N'SELECT [m].[MyEntityID], [m].[PropA]
FROM [dbo].[MyEntity] AS [m]
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT I.value(''. cast as xs:integer?'', ''int'') AS [V] FROM @p0.nodes(''/R/V'') N(I)
    ) AS [q]
    WHERE [q].[V] = [m].[MyEntityID]) OR ([m].[PropB] = @__p_1)',N'@p0 xml,@__p_1 bigint',@p0=@p3,@__p_1=100
```
Now, let's add another item to the list of values and execute the query again:
```tsql
declare @p3 xml
set @p3=convert(xml,N'<R><V>1</V><V>2</V><V>3</V><V>4</V></R>')
exec sp_executesql N'SELECT [m].[MyEntityID], [m].[PropA]
FROM [dbo].[MyEntity] AS [m]
WHERE EXISTS (
    SELECT 1
    FROM (
        SELECT I.value(''. cast as xs:integer?'', ''int'') AS [V] FROM @p0.nodes(''/R/V'') N(I)
    ) AS [q]
    WHERE [q].[V] = [m].[MyEntityID]) OR ([m].[PropB] = @__p_1)',N'@p0 xml,@__p_1 bigint',@p0=@p3,@__p_1=100
```
Great! The SQL statement provided to [sp_executesql] remains constant. In this case SQL Server can reuse the [execution plan] from the previous execution.

## The Numbers 📊
You don't have to take my word for it! Let's see a trace of what's going on under the hood when both of these queries are executed multiple times, adding a new value to the list after each execution. First, five executions of the one making direct use of the [Contains][ContainsEnumerable] LINQ method (orange), and then five executions of the second one making use of the `AsQueryableValues` extension method on the [DbContext] (green):

![Trace](/docs/images/as-queryable-trace.png)
<sup>Queries executed against SQL Server 2017 Express (14.0.2037) running on a resource constrained laptop.</sup>

As expected, none of the queries in the orange section hit the cache. On the other hand, after the first query in the green section, all the subsequent ones hit the cache and consumed fewer resources.

Now, focus your attention to the first query of the green section. Here you can observe that there's a cost associated with this technique, but this cost can be offset in the long run, especially when your queries are not trivial like the ones in these examples.

## What Makes This Work? 🤓
QueryableValues makes use of the XML parsing capabilities in SQL Server, which are available in all the supported versions of SQL Server to date. The provided sequence of values are serialized as XML and embedded in the underlying SQL query using a native XML parameter, then it uses SQL Server's XML type methods to project the query in a way that can be mapped by [Entity Framework Core].

This is a technique that I have not seen being used by other popular libraries that aim to solve this problem. It is superior from a latency standpoint because it resolves the query with a single round trip to the database and most importantly, it preserves the query's [execution plan] even when the content of the XML is changed.

## One More Thing 👀
The `AsQueryableValues` extension method allows you to treat a sequence of values as you normally would if these were another entity in your [DbContext]. The type returned by the extension is an [IQueryable&lt;T&gt;] that can be composed with other entities in your query.

For example, you can do one or more joins like this and it is totally fine:
```c#
var myQuery =
    from i in dbContext.MyEntities
    join v in dbContext.AsQueryableValues(values) on i.MyEntityID equals v
    join v2 in dbContext.AsQueryableValues(values2) on i.PropB equals v2
    select new
    {
        i.MyEntityID,
        i.PropA
    };
```
## Did You Find a 🐛 or Have an 💡?
PRs are welcome! 🙂

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
[readme-background]: #background-
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
[BuyMeACoffeeButton]: /docs/images/bmc-48.svg
[Repository]: https://github.com/yv989c/BlazarTech.QueryableValues

[benchmarks]: /benchmarks/QueryableValues.SqlServer.Benchmarks
[BenchmarksInt32]: /docs/images/benchmarks/int32-v6.3.0.png
[BenchmarksGuid]: /docs/images/benchmarks/guid-v6.3.0.png
