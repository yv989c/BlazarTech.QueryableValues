<p align="center">
    <img src="/docs/images/icon.png" alt="Logo" style="width: 80px;">
</p>

# QueryableValues

[![MIT License](https://badgen.net/badge/license/MIT/blue)](https://github.com/yv989c/BlazarTech.QueryableValues/blob/main/LICENSE.md)
[![GitHub Stars](https://badgen.net/github/stars/yv989c/BlazarTech.QueryableValues?icon=github)][Repository]
[![Nuget Downloads](https://badgen.net/nuget/dt/BlazarTech.QueryableValues.SqlServer?icon=nuget)][NuGet Package]

> 🤔💭 TLDR; By using QueryableValues, you can incorporate in-memory collections into your EF queries with outstanding performance and flexibility.

This library allows you to efficiently compose an [IEnumerable&lt;T&gt;] in your [Entity Framework Core] queries when using the [SQL Server Database Provider]. You can accomplish this by using the `AsQueryableValues` extension method that's available on the [DbContext] class. The query is processed in a single round trip to the server, in a way that preserves its [execution plan], even when the values within the [IEnumerable&lt;T&gt;] are changed on subsequent executions.

**Highlights**
- ✨ Enables the composition of in-memory data within your queries, utilizing both simple and complex types.
- 👌 Works with all versions of SQL Server supported by [Entity Framework Core].
- ⚡ Automatically uses the most efficient strategy compatible with your SQL Server instance and database configuration.
- ✅ Boasts over 140 tests for reliability and compatibility, giving you added confidence.

For a detailed explanation of the problem solved by QueryableValues, please continue reading [here][readme-background].

> 💡 Still on Entity Framework 6 (non-core)? Then [QueryableValues `EF6 Edition`](https://github.com/yv989c/BlazarTech.QueryableValues.EF6) is what you need.

## When Should You Use It?
The `AsQueryableValues` extension method is intended for queries that are dependent upon a *non-constant* sequence of external values. It provides a solution to the following [EF Core issue](https://github.com/dotnet/efcore/issues/13617) and enables other currently unsupported scenarios; like the ability to efficiently create joins with in-memory data.

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
> 💡 Pro-tip: `UseQueryableValues` offers an optional `options` delegate for additional configurations.

## How Do You Use It?
The `AsQueryableValues` extension method is provided by the `BlazarTech.QueryableValues` namespace; therefore, you must add the following `using` directive to your source code file for it to appear as a method of your [DbContext] instance:
```c#
using BlazarTech.QueryableValues;
```

> 💡 If you access your [DbContext] via an interface, you can also make the `AsQueryableValues` extension methods available on it by inheriting from the `IQueryableValuesEnabledDbContext` interface.

Below are a few examples composing a query using the values provided by an [IEnumerable&lt;T&gt;].

### Simple Type Examples

> 💡 Supported types:
> [Byte], [Int16], [Int32], [Int64], [Decimal], [Single], [Double], [DateTime], [DateTimeOffset], [Guid], [Char], and [String].

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

> 💡 Requirements:
> - Can be an anonymous type.
> - Can be a user-defined class or struct with read/write properties and a public constructor.
> - Must have one or more simple type properties, including [Boolean].

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

[![Benchmarks Chart][BenchmarksChart]][BenchmarksChartInteractive]

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

> 🎉 QueryableValues now supports JSON serialization, which improves its performance compared to using XML. By default, QueryableValues will attempt to use JSON if it is supported by your SQL Server instance and database configuration.

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
[BenchmarksChartInteractive]: https://chartbenchmark.net/?src=repo#shared=%7B%22results%22%3A%22BenchmarkDotNet%3Dv0.13.5%2C%20OS%3DWindows%2011%20(10.0.22621.1413%2F22H2%2F2022Update%2FSunValley2)%5CnAMD%20Ryzen%209%206900HS%20Creator%20Edition%2C%201%20CPU%2C%2016%20logical%20and%208%20physical%20cores%5Cn.NET%20SDK%3D7.0.202%5Cn%20%20%5BHost%5D%20%20%20%20%20%3A%20.NET%206.0.15%20(6.0.1523.11507)%2C%20X64%20RyuJIT%20AVX2%5Cn%20%20Job-OFVMJD%20%3A%20.NET%206.0.15%20(6.0.1523.11507)%2C%20X64%20RyuJIT%20AVX2%5Cn%5CnServer%3DTrue%20%20InvocationCount%3D200%20%20IterationCount%3D25%5CnRunStrategy%3DMonitoring%20%20UnrollFactor%3D1%20%20WarmupCount%3D1%5Cn%5Cn%7C%20%20%20Method%20%7C%20%20%20Type%20%7C%20NumberOfValues%20%7C%20%20%20%20%20%20%20%20%20Mean%20%7C%20%20%20%20%20Error%20%7C%20%20%20%20StdDev%20%7C%20%20%20%20%20%20%20Median%20%7C%20Ratio%20%7C%20RatioSD%20%7C%20%20%20Gen0%20%7C%20%20%20Gen1%20%7C%20%20%20Gen2%20%7C%20%20Allocated%20%7C%20Alloc%20Ratio%20%7C%5Cn%7C---------%20%7C-------%20%7C---------------%20%7C-------------%3A%7C----------%3A%7C----------%3A%7C-------------%3A%7C------%3A%7C--------%3A%7C-------%3A%7C-------%3A%7C-------%3A%7C-----------%3A%7C------------%3A%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20824.3%20us%20%7C%20%2026.03%20us%20%7C%20%2034.75%20us%20%7C%20%20%20%20%20808.9%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2020.26%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20508.7%20us%20%7C%20%2032.46%20us%20%7C%20%2043.34%20us%20%7C%20%20%20%20%20504.3%20us%20%7C%20%200.62%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.37%20KB%20%7C%20%20%20%20%20%20%20%202.04%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20431.7%20us%20%7C%20%2035.52%20us%20%7C%20%2047.41%20us%20%7C%20%20%20%20%20446.8%20us%20%7C%20%200.52%20%7C%20%20%20%200.05%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%2041.5%20KB%20%7C%20%20%20%20%20%20%20%202.05%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20964.8%20us%20%7C%20%2025.05%20us%20%7C%20%2033.44%20us%20%7C%20%20%20%20%20954.6%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2021.17%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20548.2%20us%20%7C%20%2034.29%20us%20%7C%20%2045.78%20us%20%7C%20%20%20%20%20537.0%20us%20%7C%20%200.57%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.33%20KB%20%7C%20%20%20%20%20%20%20%201.95%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20445.1%20us%20%7C%20%2034.28%20us%20%7C%20%2045.76%20us%20%7C%20%20%20%20%20453.6%20us%20%7C%20%200.46%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.56%20KB%20%7C%20%20%20%20%20%20%20%201.96%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%201%2C519.3%20us%20%7C%20%2034.23%20us%20%7C%20%2045.69%20us%20%7C%20%20%201%2C494.4%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2025.45%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%20%20%20687.5%20us%20%7C%20%2032.29%20us%20%7C%20%2043.10%20us%20%7C%20%20%20%20%20664.9%20us%20%7C%20%200.45%20%7C%20%20%20%200.03%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.52%20KB%20%7C%20%20%20%20%20%20%20%201.63%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%20%20%20448.1%20us%20%7C%20%2038.22%20us%20%7C%20%2051.03%20us%20%7C%20%20%20%20%20425.9%20us%20%7C%20%200.30%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.61%20KB%20%7C%20%20%20%20%20%20%20%201.63%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%205%2C470.2%20us%20%7C%20%2025.34%20us%20%7C%20%2033.83%20us%20%7C%20%20%205%2C473.2%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.18%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%201%2C334.4%20us%20%7C%20%2037.80%20us%20%7C%20%2050.47%20us%20%7C%20%20%201%2C316.5%20us%20%7C%20%200.24%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2044.02%20KB%20%7C%20%20%20%20%20%20%20%201.07%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%20%20%20498.9%20us%20%7C%20%2033.69%20us%20%7C%20%2044.97%20us%20%7C%20%20%20%20%20498.1%20us%20%7C%20%200.09%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2042.53%20KB%20%7C%20%20%20%20%20%20%20%201.03%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%2017%2C572.2%20us%20%7C%20%2068.50%20us%20%7C%20%2091.45%20us%20%7C%20%2017%2C566.4%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20105.67%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%204%2C016.2%20us%20%7C%20%2030.74%20us%20%7C%20%2041.04%20us%20%7C%20%20%204%2C014.4%20us%20%7C%20%200.23%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2052.18%20KB%20%7C%20%20%20%20%20%20%20%200.49%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%20%20%20685.0%20us%20%7C%20%2030.40%20us%20%7C%20%2040.59%20us%20%7C%20%20%20%20%20661.9%20us%20%7C%20%200.04%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2046.37%20KB%20%7C%20%20%20%20%20%20%20%200.44%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%2071%2C616.8%20us%20%7C%20677.00%20us%20%7C%20903.77%20us%20%7C%20%2071%2C227.6%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20363.17%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%2014%2C045.8%20us%20%7C%20%2050.55%20us%20%7C%20%2067.48%20us%20%7C%20%2014%2C029.9%20us%20%7C%20%200.20%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2084.85%20KB%20%7C%20%20%20%20%20%20%20%200.23%20%7C%5Cn%7C%20WithJson%20%7C%20%20Int32%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%20%201%2C577.1%20us%20%7C%20%2032.17%20us%20%7C%20%2042.95%20us%20%7C%20%20%201%2C564.8%20us%20%7C%20%200.02%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2061.07%20KB%20%7C%20%20%20%20%20%20%20%200.17%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20788.9%20us%20%7C%20%2020.31%20us%20%7C%20%2027.11%20us%20%7C%20%20%20%20%20778.1%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2020.74%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20487.6%20us%20%7C%20%2030.51%20us%20%7C%20%2040.74%20us%20%7C%20%20%20%20%20487.7%20us%20%7C%20%200.62%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.23%20KB%20%7C%20%20%20%20%20%20%20%201.99%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20434.7%20us%20%7C%20%2033.42%20us%20%7C%20%2044.61%20us%20%7C%20%20%20%20%20443.3%20us%20%7C%20%200.55%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.19%20KB%20%7C%20%20%20%20%20%20%20%201.99%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20939.1%20us%20%7C%20%2029.24%20us%20%7C%20%2039.04%20us%20%7C%20%20%20%20%20921.1%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2023.49%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20515.1%20us%20%7C%20%2032.95%20us%20%7C%20%2043.99%20us%20%7C%20%20%20%20%20509.2%20us%20%7C%20%200.55%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2042.23%20KB%20%7C%20%20%20%20%20%20%20%201.80%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20450.0%20us%20%7C%20%2033.55%20us%20%7C%20%2044.79%20us%20%7C%20%20%20%20%20461.4%20us%20%7C%20%200.48%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.98%20KB%20%7C%20%20%20%20%20%20%20%201.79%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%201%2C566.2%20us%20%7C%20%2043.12%20us%20%7C%20%2057.56%20us%20%7C%20%20%201%2C551.3%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2033.24%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%20%20%20607.3%20us%20%7C%20%2033.01%20us%20%7C%20%2044.07%20us%20%7C%20%20%20%20%20587.0%20us%20%7C%20%200.39%20%7C%20%20%20%200.03%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2043.58%20KB%20%7C%20%20%20%20%20%20%20%201.31%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%20%20%20488.4%20us%20%7C%20%2032.86%20us%20%7C%20%2043.87%20us%20%7C%20%20%20%20%20487.3%20us%20%7C%20%200.31%20%7C%20%20%20%200.03%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2043.48%20KB%20%7C%20%20%20%20%20%20%20%201.31%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%205%2C140.0%20us%20%7C%20%2052.22%20us%20%7C%20%2069.71%20us%20%7C%20%20%205%2C138.2%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2074.11%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%20%20%20987.8%20us%20%7C%20%2037.30%20us%20%7C%20%2049.79%20us%20%7C%20%20%20%20%20965.0%20us%20%7C%20%200.19%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2051.97%20KB%20%7C%20%20%20%20%20%20%20%200.70%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%20%20%20665.9%20us%20%7C%20%2038.37%20us%20%7C%20%2051.23%20us%20%7C%20%20%20%20%20636.8%20us%20%7C%20%200.13%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2051.12%20KB%20%7C%20%20%20%20%20%20%20%200.69%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%2016%2C031.0%20us%20%7C%20%2074.08%20us%20%7C%20%2098.89%20us%20%7C%20%2016%2C023.7%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20219.5%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%202%2C528.8%20us%20%7C%20%2038.80%20us%20%7C%20%2051.79%20us%20%7C%20%20%202%2C517.7%20us%20%7C%20%200.16%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2084.36%20KB%20%7C%20%20%20%20%20%20%20%200.38%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%201%2C368.8%20us%20%7C%20%2022.42%20us%20%7C%20%2029.93%20us%20%7C%20%20%201%2C355.1%20us%20%7C%20%200.09%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2080.08%20KB%20%7C%20%20%20%20%20%20%20%200.36%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%2071%2C956.6%20us%20%7C%20688.35%20us%20%7C%20918.93%20us%20%7C%20%2072%2C148.6%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20801.13%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%20%209%2C399.9%20us%20%7C%20%2076.33%20us%20%7C%20101.90%20us%20%7C%20%20%209%2C359.8%20us%20%7C%20%200.13%20%7C%20%20%20%200.00%20%7C%205.0000%20%7C%205.0000%20%7C%205.0000%20%7C%20%20213.42%20KB%20%7C%20%20%20%20%20%20%20%200.27%20%7C%5Cn%7C%20WithJson%20%7C%20%20%20Guid%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%20%204%2C463.6%20us%20%7C%20%2036.90%20us%20%7C%20%2049.26%20us%20%7C%20%20%204%2C442.6%20us%20%7C%20%200.06%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20197.4%20KB%20%7C%20%20%20%20%20%20%20%200.25%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20858.7%20us%20%7C%20%2023.34%20us%20%7C%20%2031.16%20us%20%7C%20%20%20%20%20846.2%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2021.44%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20637.4%20us%20%7C%20%2035.57%20us%20%7C%20%2047.48%20us%20%7C%20%20%20%20%20626.0%20us%20%7C%20%200.74%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2055.52%20KB%20%7C%20%20%20%20%20%20%20%202.59%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%202%20%7C%20%20%20%20%20534.5%20us%20%7C%20%2030.81%20us%20%7C%20%2041.13%20us%20%7C%20%20%20%20%20528.7%20us%20%7C%20%200.62%20%7C%20%20%20%200.03%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2042.83%20KB%20%7C%20%20%20%20%20%20%20%202.00%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%201%2C028.9%20us%20%7C%20%2024.07%20us%20%7C%20%2032.13%20us%20%7C%20%20%201%2C015.2%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2025.55%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20737.8%20us%20%7C%20%2044.23%20us%20%7C%20%2059.05%20us%20%7C%20%20%20%20%20727.5%20us%20%7C%20%200.72%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2056.98%20KB%20%7C%20%20%20%20%20%20%20%202.23%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%208%20%7C%20%20%20%20%20641.8%20us%20%7C%20%2034.63%20us%20%7C%20%2046.23%20us%20%7C%20%20%20%20%20640.1%20us%20%7C%20%200.62%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2043.64%20KB%20%7C%20%20%20%20%20%20%20%201.71%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%201%2C692.5%20us%20%7C%20%2023.43%20us%20%7C%20%2031.27%20us%20%7C%20%20%201%2C684.7%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2041.84%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%201%2C016.7%20us%20%7C%20%2056.75%20us%20%7C%20%2075.76%20us%20%7C%20%20%20%20%20976.6%20us%20%7C%20%200.60%20%7C%20%20%20%200.04%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2060.35%20KB%20%7C%20%20%20%20%20%20%20%201.44%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%2032%20%7C%20%20%20%20%20871.5%20us%20%7C%20%2039.02%20us%20%7C%20%2052.10%20us%20%7C%20%20%20%20%20843.8%20us%20%7C%20%200.51%20%7C%20%20%20%200.03%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2047.29%20KB%20%7C%20%20%20%20%20%20%20%201.13%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%207%2C665.5%20us%20%7C%20%2028.53%20us%20%7C%20%2038.09%20us%20%7C%20%20%207%2C662.0%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20103.65%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%202%2C392.2%20us%20%7C%20%2035.64%20us%20%7C%20%2047.57%20us%20%7C%20%20%202%2C379.7%20us%20%7C%20%200.31%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%2074.85%20KB%20%7C%20%20%20%20%20%20%20%200.72%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20128%20%7C%20%20%202%2C063.6%20us%20%7C%20%2026.61%20us%20%7C%20%2035.53%20us%20%7C%20%20%202%2C063.5%20us%20%7C%20%200.27%20%7C%20%20%20%200.01%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%2061.2%20KB%20%7C%20%20%20%20%20%20%20%200.59%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%2026%2C444.7%20us%20%7C%20102.44%20us%20%7C%20136.75%20us%20%7C%20%2026%2C421.0%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20343.51%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%208%2C134.2%20us%20%7C%20%2032.51%20us%20%7C%20%2043.41%20us%20%7C%20%20%208%2C125.8%20us%20%7C%20%200.31%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20132.34%20KB%20%7C%20%20%20%20%20%20%20%200.39%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%20%20512%20%7C%20%20%207%2C210.9%20us%20%7C%20%2033.10%20us%20%7C%20%2044.18%20us%20%7C%20%20%207%2C199.6%20us%20%7C%20%200.27%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20116.42%20KB%20%7C%20%20%20%20%20%20%20%200.34%20%7C%5Cn%7C%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%7C%20%20%20%20%20%20%20%20%20%20%20%20%20%7C%5Cn%7C%20%20Without%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20112%2C512.8%20us%20%7C%20443.78%20us%20%7C%20592.43%20us%20%7C%20112%2C461.1%20us%20%7C%20%201.00%20%7C%20%20%20%200.00%20%7C%205.0000%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%201310.32%20KB%20%7C%20%20%20%20%20%20%20%201.00%20%7C%5Cn%7C%20%20WithXml%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%2032%2C080.3%20us%20%7C%20138.18%20us%20%7C%20184.47%20us%20%7C%20%2032%2C075.1%20us%20%7C%20%200.29%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20361.05%20KB%20%7C%20%20%20%20%20%20%20%200.28%20%7C%5Cn%7C%20WithJson%20%7C%20String%20%7C%20%20%20%20%20%20%20%20%20%20%202048%20%7C%20%2028%2C929.1%20us%20%7C%20%2084.67%20us%20%7C%20113.03%20us%20%7C%20%2028%2C917.8%20us%20%7C%20%200.26%20%7C%20%20%20%200.00%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20%20%20%20%20-%20%7C%20%20336.47%20KB%20%7C%20%20%20%20%20%20%20%200.26%20%7C%5Cn%22%2C%22settings%22%3A%7B%22display%22%3A%22Duration%22%2C%22scale%22%3A%22Log2%22%2C%22theme%22%3A%22Dark%22%7D%7D "Click for interactive chart"