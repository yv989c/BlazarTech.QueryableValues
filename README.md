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
The following [benchmarks] consist of simple EF Core queries that have a dependency on a random sequence of [Int32], [Guid], and [String] values via the `Contains` LINQ method. It shows the performance differences between not using and using QueryableValues. In practice, the benefits of using QueryableValues are more dramatic on complex EF Core queries and busy environments.

### Benchmarked Libraries
| Package | Version |
| ------- |:-------:|
| Microsoft.EntityFrameworkCore.SqlServer | 7.0.4 |
| BlazarTech.QueryableValues.SqlServer | 7.2.0 |

### BenchmarkDotNet System Specs and Configuration
```
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
AMD Ryzen 9 6900HS Creator Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2
  Job-OFVMJD : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2

Server=True  InvocationCount=200  IterationCount=25
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=1
```
### SQL Server Instance Specs
```
Microsoft SQL Server 2022 (RTM) - 16.0.1000.6 (X64)
Oct  8 2022 05:58:25
Copyright (C) 2022 Microsoft Corporation
Express Edition (64-bit) on Windows 10 Pro 10.0 <X64> (Build 22621: ) (Hypervisor)
```
- The SQL Server instance was running in the same system where the benchmarks were executed.
- Shared Memory is the only network protocol that's enabled on this instance.

### Query Duration - Without vs. With (XML) vs. With (JSON)

**Legend:**

- **Without:** Plain EF.
- **With (XML):** EF with QueryableValues using the XML serializer.
- **With (JSON):** EF with QueryableValues using the JSON serializer.

![BenchmarksChart][BenchmarksChart]

<details>

|   Method |   Type | NumberOfValues |         Mean |     Error |    StdDev |       Median | Ratio | RatioSD |   Gen0 |   Gen1 |   Gen2 |  Allocated | Alloc Ratio |
|--------- |------- |--------------- |-------------:|----------:|----------:|-------------:|------:|--------:|-------:|-------:|-------:|-----------:|------------:|
|  Without |  Int32 |              2 |     824.3 us |  26.03 us |  34.75 us |     808.9 us |  1.00 |    0.00 |      - |      - |      - |   20.26 KB |        1.00 |
|  WithXml |  Int32 |              2 |     508.7 us |  32.46 us |  43.34 us |     504.3 us |  0.62 |    0.04 |      - |      - |      - |   41.37 KB |        2.04 |
| WithJson |  Int32 |              2 |     431.7 us |  35.52 us |  47.41 us |     446.8 us |  0.52 |    0.05 |      - |      - |      - |    41.5 KB |        2.05 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |  Int32 |              8 |     964.8 us |  25.05 us |  33.44 us |     954.6 us |  1.00 |    0.00 |      - |      - |      - |   21.17 KB |        1.00 |
|  WithXml |  Int32 |              8 |     548.2 us |  34.29 us |  45.78 us |     537.0 us |  0.57 |    0.04 |      - |      - |      - |   41.33 KB |        1.95 |
| WithJson |  Int32 |              8 |     445.1 us |  34.28 us |  45.76 us |     453.6 us |  0.46 |    0.04 |      - |      - |      - |   41.56 KB |        1.96 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |  Int32 |             32 |   1,519.3 us |  34.23 us |  45.69 us |   1,494.4 us |  1.00 |    0.00 |      - |      - |      - |   25.45 KB |        1.00 |
|  WithXml |  Int32 |             32 |     687.5 us |  32.29 us |  43.10 us |     664.9 us |  0.45 |    0.03 |      - |      - |      - |   41.52 KB |        1.63 |
| WithJson |  Int32 |             32 |     448.1 us |  38.22 us |  51.03 us |     425.9 us |  0.30 |    0.04 |      - |      - |      - |   41.61 KB |        1.63 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |  Int32 |            128 |   5,470.2 us |  25.34 us |  33.83 us |   5,473.2 us |  1.00 |    0.00 |      - |      - |      - |   41.18 KB |        1.00 |
|  WithXml |  Int32 |            128 |   1,334.4 us |  37.80 us |  50.47 us |   1,316.5 us |  0.24 |    0.01 |      - |      - |      - |   44.02 KB |        1.07 |
| WithJson |  Int32 |            128 |     498.9 us |  33.69 us |  44.97 us |     498.1 us |  0.09 |    0.01 |      - |      - |      - |   42.53 KB |        1.03 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |  Int32 |            512 |  17,572.2 us |  68.50 us |  91.45 us |  17,566.4 us |  1.00 |    0.00 |      - |      - |      - |  105.67 KB |        1.00 |
|  WithXml |  Int32 |            512 |   4,016.2 us |  30.74 us |  41.04 us |   4,014.4 us |  0.23 |    0.00 |      - |      - |      - |   52.18 KB |        0.49 |
| WithJson |  Int32 |            512 |     685.0 us |  30.40 us |  40.59 us |     661.9 us |  0.04 |    0.00 |      - |      - |      - |   46.37 KB |        0.44 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |  Int32 |           2048 |  71,616.8 us | 677.00 us | 903.77 us |  71,227.6 us |  1.00 |    0.00 |      - |      - |      - |  363.17 KB |        1.00 |
|  WithXml |  Int32 |           2048 |  14,045.8 us |  50.55 us |  67.48 us |  14,029.9 us |  0.20 |    0.00 |      - |      - |      - |   84.85 KB |        0.23 |
| WithJson |  Int32 |           2048 |   1,577.1 us |  32.17 us |  42.95 us |   1,564.8 us |  0.02 |    0.00 |      - |      - |      - |   61.07 KB |        0.17 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |              2 |     788.9 us |  20.31 us |  27.11 us |     778.1 us |  1.00 |    0.00 |      - |      - |      - |   20.74 KB |        1.00 |
|  WithXml |   Guid |              2 |     487.6 us |  30.51 us |  40.74 us |     487.7 us |  0.62 |    0.04 |      - |      - |      - |   41.23 KB |        1.99 |
| WithJson |   Guid |              2 |     434.7 us |  33.42 us |  44.61 us |     443.3 us |  0.55 |    0.04 |      - |      - |      - |   41.19 KB |        1.99 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |              8 |     939.1 us |  29.24 us |  39.04 us |     921.1 us |  1.00 |    0.00 |      - |      - |      - |   23.49 KB |        1.00 |
|  WithXml |   Guid |              8 |     515.1 us |  32.95 us |  43.99 us |     509.2 us |  0.55 |    0.04 |      - |      - |      - |   42.23 KB |        1.80 |
| WithJson |   Guid |              8 |     450.0 us |  33.55 us |  44.79 us |     461.4 us |  0.48 |    0.04 |      - |      - |      - |   41.98 KB |        1.79 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |             32 |   1,566.2 us |  43.12 us |  57.56 us |   1,551.3 us |  1.00 |    0.00 |      - |      - |      - |   33.24 KB |        1.00 |
|  WithXml |   Guid |             32 |     607.3 us |  33.01 us |  44.07 us |     587.0 us |  0.39 |    0.03 |      - |      - |      - |   43.58 KB |        1.31 |
| WithJson |   Guid |             32 |     488.4 us |  32.86 us |  43.87 us |     487.3 us |  0.31 |    0.03 |      - |      - |      - |   43.48 KB |        1.31 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |            128 |   5,140.0 us |  52.22 us |  69.71 us |   5,138.2 us |  1.00 |    0.00 |      - |      - |      - |   74.11 KB |        1.00 |
|  WithXml |   Guid |            128 |     987.8 us |  37.30 us |  49.79 us |     965.0 us |  0.19 |    0.01 |      - |      - |      - |   51.97 KB |        0.70 |
| WithJson |   Guid |            128 |     665.9 us |  38.37 us |  51.23 us |     636.8 us |  0.13 |    0.01 |      - |      - |      - |   51.12 KB |        0.69 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |            512 |  16,031.0 us |  74.08 us |  98.89 us |  16,023.7 us |  1.00 |    0.00 |      - |      - |      - |   219.5 KB |        1.00 |
|  WithXml |   Guid |            512 |   2,528.8 us |  38.80 us |  51.79 us |   2,517.7 us |  0.16 |    0.00 |      - |      - |      - |   84.36 KB |        0.38 |
| WithJson |   Guid |            512 |   1,368.8 us |  22.42 us |  29.93 us |   1,355.1 us |  0.09 |    0.00 |      - |      - |      - |   80.08 KB |        0.36 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without |   Guid |           2048 |  71,956.6 us | 688.35 us | 918.93 us |  72,148.6 us |  1.00 |    0.00 |      - |      - |      - |  801.13 KB |        1.00 |
|  WithXml |   Guid |           2048 |   9,399.9 us |  76.33 us | 101.90 us |   9,359.8 us |  0.13 |    0.00 | 5.0000 | 5.0000 | 5.0000 |  213.42 KB |        0.27 |
| WithJson |   Guid |           2048 |   4,463.6 us |  36.90 us |  49.26 us |   4,442.6 us |  0.06 |    0.00 |      - |      - |      - |   197.4 KB |        0.25 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |              2 |     858.7 us |  23.34 us |  31.16 us |     846.2 us |  1.00 |    0.00 |      - |      - |      - |   21.44 KB |        1.00 |
|  WithXml | String |              2 |     637.4 us |  35.57 us |  47.48 us |     626.0 us |  0.74 |    0.04 |      - |      - |      - |   55.52 KB |        2.59 |
| WithJson | String |              2 |     534.5 us |  30.81 us |  41.13 us |     528.7 us |  0.62 |    0.03 |      - |      - |      - |   42.83 KB |        2.00 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |              8 |   1,028.9 us |  24.07 us |  32.13 us |   1,015.2 us |  1.00 |    0.00 |      - |      - |      - |   25.55 KB |        1.00 |
|  WithXml | String |              8 |     737.8 us |  44.23 us |  59.05 us |     727.5 us |  0.72 |    0.04 |      - |      - |      - |   56.98 KB |        2.23 |
| WithJson | String |              8 |     641.8 us |  34.63 us |  46.23 us |     640.1 us |  0.62 |    0.04 |      - |      - |      - |   43.64 KB |        1.71 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |             32 |   1,692.5 us |  23.43 us |  31.27 us |   1,684.7 us |  1.00 |    0.00 |      - |      - |      - |   41.84 KB |        1.00 |
|  WithXml | String |             32 |   1,016.7 us |  56.75 us |  75.76 us |     976.6 us |  0.60 |    0.04 |      - |      - |      - |   60.35 KB |        1.44 |
| WithJson | String |             32 |     871.5 us |  39.02 us |  52.10 us |     843.8 us |  0.51 |    0.03 |      - |      - |      - |   47.29 KB |        1.13 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |            128 |   7,665.5 us |  28.53 us |  38.09 us |   7,662.0 us |  1.00 |    0.00 |      - |      - |      - |  103.65 KB |        1.00 |
|  WithXml | String |            128 |   2,392.2 us |  35.64 us |  47.57 us |   2,379.7 us |  0.31 |    0.01 |      - |      - |      - |   74.85 KB |        0.72 |
| WithJson | String |            128 |   2,063.6 us |  26.61 us |  35.53 us |   2,063.5 us |  0.27 |    0.01 |      - |      - |      - |    61.2 KB |        0.59 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |            512 |  26,444.7 us | 102.44 us | 136.75 us |  26,421.0 us |  1.00 |    0.00 |      - |      - |      - |  343.51 KB |        1.00 |
|  WithXml | String |            512 |   8,134.2 us |  32.51 us |  43.41 us |   8,125.8 us |  0.31 |    0.00 |      - |      - |      - |  132.34 KB |        0.39 |
| WithJson | String |            512 |   7,210.9 us |  33.10 us |  44.18 us |   7,199.6 us |  0.27 |    0.00 |      - |      - |      - |  116.42 KB |        0.34 |
|          |        |                |              |           |           |              |       |         |        |        |        |            |             |
|  Without | String |           2048 | 112,512.8 us | 443.78 us | 592.43 us | 112,461.1 us |  1.00 |    0.00 | 5.0000 |      - |      - | 1310.32 KB |        1.00 |
|  WithXml | String |           2048 |  32,080.3 us | 138.18 us | 184.47 us |  32,075.1 us |  0.29 |    0.00 |      - |      - |      - |  361.05 KB |        0.28 |
| WithJson | String |           2048 |  28,929.1 us |  84.67 us | 113.03 us |  28,917.8 us |  0.26 |    0.00 |      - |      - |      - |  336.47 KB |        0.26 |

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

> 🎉 QueryableValues now supports JSON serialization, which improves its performance compared to using XML. By default, QueryableValues will attempt to use JSON if it is supported.

QueryableValues makes use of the XML parsing capabilities in SQL Server, which are available in all the supported versions of SQL Server to date. The provided sequence of values are serialized as XML and embedded in the underlying SQL query using a native XML parameter, then it uses SQL Server's XML type methods to project the query in a way that can be mapped by [Entity Framework Core].

This is a technique that I have not seen being used by other popular libraries that aim to solve this problem. It is superior from a latency standpoint because it resolves the query with a single round trip to the database and most importantly, it preserves the query's [execution plan] even when the content of the XML is changed.

## One More Thing 👀
The `AsQueryableValues` extension method allows you to treat a sequence of values as you normally would if they were another entity in your [DbContext]. The type returned by the extension is an [IQueryable&lt;T&gt;] that can be composed with other entities in your query.

For example, you can perform one or more joins like this and it is completely fine:
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

Isn't that great? 🥰

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
[BenchmarksChart]: /docs/images/benchmarks/v7.2.0.png