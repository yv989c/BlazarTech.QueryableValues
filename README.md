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
- ⚡ Automatically uses the most efficient strategy compatible with your SQL Server instance and configuration.
- ✅ Boasts over 140 tests for reliability and compatibility, giving you added confidence.

For a detailed explanation of the problem solved by QueryableValues, please continue reading [here][readme-background].

> 💡 Still on Entity Framework 6 (non-core)? Then [QueryableValues `EF6 Edition`](https://github.com/yv989c/BlazarTech.QueryableValues.EF6) is what you need.

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
> 💡 `UseQueryableValues` offers an optional `options` delegate for additional configurations.

## How Do You Use It?
The `AsQueryableValues` extension method is provided by the `BlazarTech.QueryableValues` namespace; therefore, you must add the following `using` directive to your source code file for it to appear as a method of your [DbContext] instance:
```c#
using BlazarTech.QueryableValues;
```

> 💡 If you access your [DbContext] via an interface, you can also make the `AsQueryableValues` extension methods available on it by inheriting from the `IQueryableValuesEnabledDbContext` interface.

Below are a few examples composing a query using the values provided by an [IEnumerable&lt;T&gt;].

### Simple Type Examples

> 💡 Supports [Byte], [Int16], [Int32], [Int64], [Decimal], [Single], [Double], [DateTime], [DateTimeOffset], [DateOnly], [TimeOnly], [Guid], [Char], [String], and [Enum].

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

> 💡 Must be an anonymous or user-defined type with one or more simple type properties, including [Boolean].

```c#
// Performance Tip:
// If your IEnumerable<T> item type (T) has many properties, project only 
// the ones you need to a new variable and use it in your query.
var projectedItems = items.Select(i => new { i.CategoryId, i.ColorName });

// Example #1 (LINQ method syntax)
var myQuery1 = dbContext.Product
    .Join(
        dbContext.AsQueryableValues(projectedItems),
        p => new { p.CategoryId, p.ColorName },
        pi => new { pi.CategoryId, pi.ColorName },
        (p, pi) => new
        {
            p.ProductId,
            p.Description
        }
    );

// Example #2 (LINQ query syntax)
var myQuery2 = 
    from p in dbContext.Product
    join pi in dbContext.AsQueryableValues(projectedItems) on new { p.CategoryId, p.ColorName } equals new { pi.CategoryId, pi.ColorName }
    select new
    {
        p.ProductId,
        p.Description
    };
```

**About Complex Types**
> ⚠️ All the data provided by this type is transmitted to the server; therefore, ensure that it only contains the properties you need for your query. Not following this recommendation will degrade the query's performance.

> ⚠️ There is a limit of up to 10 properties for any given simple type (e.g. cannot have more than 10 [Int32] properties). Exceeding that limit will cause an exception and may also suggest that you should rethink your strategy.

# Benchmarks
The following [benchmarks] consist of simple EF Core queries that have a dependency on a random sequence of [Int32], [Guid], and [String] values via the `Contains` LINQ method. It shows the performance differences between not using and using QueryableValues. In practice, the benefits of using QueryableValues are more dramatic on complex EF Core queries and busy environments.

### Benchmarked Libraries
| Package | Version |
| ------- |:-------:|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 |
| BlazarTech.QueryableValues.SqlServer | 8.1.0 |

### BenchmarkDotNet System Specs and Configuration
```
BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2715/22H2/2022Update/SunValley2)
AMD Ryzen 9 6900HS Creator Edition, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-EBAAJF : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

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

| Method   | Type   | NumberOfValues | Mean           | Error       | StdDev      | Median         | Ratio | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|--------- |------- |--------------- |---------------:|------------:|------------:|---------------:|------:|--------:|-------:|-------:|-------:|----------:|------------:|
| Without  | Int32  | 2              |     1,167.3 us |    43.27 us |    57.77 us |     1,143.9 us |  1.00 |    0.00 |      - |      - |      - |    8.7 KB |        1.00 |
| WithXml  | Int32  | 2              |       526.3 us |    14.62 us |    19.51 us |       520.8 us |  0.45 |    0.03 |      - |      - |      - |  44.86 KB |        5.16 |
| WithJson | Int32  | 2              |       432.1 us |    16.57 us |    22.12 us |       427.6 us |  0.37 |    0.02 |      - |      - |      - |   31.3 KB |        3.60 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Int32  | 8              |     1,953.4 us |    54.65 us |    72.95 us |     1,959.4 us |  1.00 |    0.00 |      - |      - |      - |   8.77 KB |        1.00 |
| WithXml  | Int32  | 8              |       591.3 us |    29.30 us |    39.11 us |       595.6 us |  0.30 |    0.02 |      - |      - |      - |  45.42 KB |        5.18 |
| WithJson | Int32  | 8              |       440.7 us |    11.51 us |    15.37 us |       439.7 us |  0.23 |    0.01 |      - |      - |      - |  31.77 KB |        3.62 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Int32  | 32             |     2,662.0 us |   103.32 us |   137.93 us |     2,688.7 us |  1.00 |    0.00 |      - |      - |      - |   9.12 KB |        1.00 |
| WithXml  | Int32  | 32             |       822.3 us |    70.84 us |    94.57 us |       876.3 us |  0.31 |    0.04 |      - |      - |      - |   48.2 KB |        5.29 |
| WithJson | Int32  | 32             |       481.1 us |    21.07 us |    28.13 us |       475.6 us |  0.18 |    0.01 |      - |      - |      - |  33.88 KB |        3.72 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Int32  | 128            |     2,490.2 us |   103.31 us |   137.91 us |     2,490.8 us |  1.00 |    0.00 |      - |      - |      - |  15.37 KB |        1.00 |
| WithXml  | Int32  | 128            |     1,941.4 us |    96.33 us |   128.59 us |     1,877.3 us |  0.78 |    0.05 |      - |      - |      - |  59.58 KB |        3.88 |
| WithJson | Int32  | 128            |       604.4 us |    47.16 us |    62.96 us |       624.6 us |  0.24 |    0.03 |      - |      - |      - |   41.5 KB |        2.70 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Int32  | 512            |     2,546.6 us |   129.47 us |   172.84 us |     2,567.5 us |  1.00 |    0.00 |      - |      - |      - |   22.7 KB |        1.00 |
| WithXml  | Int32  | 512            |     6,416.5 us |    59.68 us |    79.67 us |     6,403.5 us |  2.53 |    0.17 |      - |      - |      - | 112.25 KB |        4.95 |
| WithJson | Int32  | 512            |       989.7 us |   138.60 us |   185.03 us |       900.3 us |  0.39 |    0.07 |      - |      - |      - |  73.62 KB |        3.24 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Int32  | 2048           |     2,632.8 us |   130.88 us |   174.72 us |     2,636.6 us |  1.00 |    0.00 |      - |      - |      - |  65.01 KB |        1.00 |
| WithXml  | Int32  | 2048           |    24,377.3 us |   236.77 us |   316.09 us |    24,251.8 us |  9.30 |    0.64 |      - |      - |      - | 346.68 KB |        5.33 |
| WithJson | Int32  | 2048           |     2,601.5 us |   225.17 us |   300.60 us |     2,479.2 us |  0.99 |    0.13 |      - |      - |      - | 204.41 KB |        3.14 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 2              |     2,058.0 us |    32.38 us |    43.22 us |     2,047.2 us |  1.00 |    0.00 |      - |      - |      - |   8.92 KB |        1.00 |
| WithXml  | Guid   | 2              |       517.1 us |    18.83 us |    25.14 us |       515.7 us |  0.25 |    0.01 |      - |      - |      - |  45.23 KB |        5.07 |
| WithJson | Guid   | 2              |       440.0 us |    16.36 us |    21.84 us |       438.1 us |  0.21 |    0.01 |      - |      - |      - |  31.56 KB |        3.54 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 8              |     5,811.5 us |    30.72 us |    41.01 us |     5,806.7 us |  1.00 |    0.00 |      - |      - |      - |  14.17 KB |        1.00 |
| WithXml  | Guid   | 8              |       573.1 us |    32.34 us |    43.17 us |       580.0 us |  0.10 |    0.01 |      - |      - |      - |  46.64 KB |        3.29 |
| WithJson | Guid   | 8              |       461.8 us |    17.77 us |    23.72 us |       460.0 us |  0.08 |    0.00 |      - |      - |      - |  32.66 KB |        2.30 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 32             |    20,328.2 us |    63.94 us |    85.36 us |    20,340.1 us |  1.00 |    0.00 |      - |      - |      - |  17.83 KB |        1.00 |
| WithXml  | Guid   | 32             |       771.5 us |    77.67 us |   103.68 us |       838.1 us |  0.04 |    0.01 |      - |      - |      - |   52.9 KB |        2.97 |
| WithJson | Guid   | 32             |       525.1 us |    12.43 us |    16.60 us |       521.5 us |  0.03 |    0.00 |      - |      - |      - |  36.57 KB |        2.05 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 128            |    80,563.7 us |   695.46 us |   928.42 us |    80,338.0 us | 1.000 |    0.00 |      - |      - |      - |  45.43 KB |        1.00 |
| WithXml  | Guid   | 128            |     1,631.3 us |   103.83 us |   138.60 us |     1,556.0 us | 0.020 |    0.00 |      - |      - |      - |  77.96 KB |        1.72 |
| WithJson | Guid   | 128            |       740.5 us |    71.79 us |    95.83 us |       786.8 us | 0.009 |    0.00 |      - |      - |      - |  52.08 KB |        1.15 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 512            |   330,720.6 us |   646.57 us |   863.15 us |   330,831.7 us | 1.000 |    0.00 |      - |      - |      - | 133.83 KB |        1.00 |
| WithXml  | Guid   | 512            |     5,109.5 us |   146.38 us |   195.41 us |     5,056.7 us | 0.015 |    0.00 |      - |      - |      - | 184.93 KB |        1.38 |
| WithJson | Guid   | 512            |     1,547.3 us |   145.16 us |   193.78 us |     1,425.7 us | 0.005 |    0.00 |      - |      - |      - | 115.97 KB |        0.87 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | Guid   | 2048           | 1,434,232.2 us | 4,863.00 us | 6,491.96 us | 1,431,593.8 us | 1.000 |    0.00 | 5.0000 | 5.0000 | 5.0000 | 562.98 KB |        1.00 |
| WithXml  | Guid   | 2048           |    19,451.1 us |    75.68 us |   101.03 us |    19,443.0 us | 0.014 |    0.00 |      - |      - |      - | 637.14 KB |        1.13 |
| WithJson | Guid   | 2048           |     5,226.9 us |   214.80 us |   286.75 us |     5,140.3 us | 0.004 |    0.00 |      - |      - |      - | 372.87 KB |        0.66 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 2              |       976.7 us |    40.55 us |    54.14 us |       971.1 us |  1.00 |    0.00 |      - |      - |      - |   9.65 KB |        1.00 |
| WithXml  | String | 2              |       540.1 us |    13.88 us |    18.53 us |       538.6 us |  0.55 |    0.03 |      - |      - |      - |  45.82 KB |        4.75 |
| WithJson | String | 2              |       516.0 us |    21.13 us |    28.21 us |       514.0 us |  0.53 |    0.04 |      - |      - |      - |  32.14 KB |        3.33 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 8              |     2,481.9 us |    43.38 us |    57.91 us |     2,479.8 us |  1.00 |    0.00 |      - |      - |      - |  14.96 KB |        1.00 |
| WithXml  | String | 8              |       608.7 us |    30.48 us |    40.69 us |       614.0 us |  0.25 |    0.02 |      - |      - |      - |  47.42 KB |        3.17 |
| WithJson | String | 8              |       617.3 us |    18.08 us |    24.14 us |       613.0 us |  0.25 |    0.01 |      - |      - |      - |  33.66 KB |        2.25 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 32             |     8,006.9 us |    44.74 us |    59.73 us |     8,010.0 us |  1.00 |    0.00 |      - |      - |      - |  20.39 KB |        1.00 |
| WithXml  | String | 32             |       838.6 us |    83.21 us |   111.08 us |       906.2 us |  0.10 |    0.01 |      - |      - |      - |   53.9 KB |        2.64 |
| WithJson | String | 32             |       849.1 us |    64.67 us |    86.33 us |       892.0 us |  0.11 |    0.01 |      - |      - |      - |  37.95 KB |        1.86 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 128            |    31,372.5 us |    58.88 us |    78.60 us |    31,382.7 us |  1.00 |    0.00 |      - |      - |      - |  52.91 KB |        1.00 |
| WithXml  | String | 128            |     1,802.2 us |   146.18 us |   195.14 us |     1,690.7 us |  0.06 |    0.01 |      - |      - |      - |  79.74 KB |        1.51 |
| WithJson | String | 128            |     1,863.0 us |   130.62 us |   174.38 us |     1,758.8 us |  0.06 |    0.01 |      - |      - |      - |  56.59 KB |        1.07 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 512            |   133,130.3 us |   634.15 us |   846.57 us |   133,481.7 us |  1.00 |    0.00 |      - |      - |      - | 165.83 KB |        1.00 |
| WithXml  | String | 512            |     5,911.6 us |   134.15 us |   179.08 us |     5,872.8 us |  0.04 |    0.00 |      - |      - |      - | 190.35 KB |        1.15 |
| WithJson | String | 512            |     6,672.9 us |   165.38 us |   220.78 us |     6,638.3 us |  0.05 |    0.00 |      - |      - |      - | 131.24 KB |        0.79 |
|          |        |                |                |             |             |                |       |         |        |        |        |           |             |
| Without  | String | 2048           |   535,679.0 us |   977.69 us | 1,305.19 us |   535,368.9 us |  1.00 |    0.00 | 5.0000 | 5.0000 | 5.0000 |  687.4 KB |        1.00 |
| WithXml  | String | 2048           |    22,191.9 us |    65.79 us |    87.83 us |    22,189.3 us |  0.04 |    0.00 |      - |      - |      - | 655.65 KB |        0.95 |
| WithJson | String | 2048           |    27,953.3 us |   133.58 us |   178.32 us |    27,962.5 us |  0.05 |    0.00 |      - |      - |      - | 432.27 KB |        0.63 |

</details>

### Version Archive

- [`7.2.0`](/docs/benchmarks/v7.2.0.md)

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

> 💡 To address this issue, EF8 now incorporates the use of the [`OPENJSON`] function when possible. The change was tracked by this [EF Core issue](https://github.com/dotnet/efcore/issues/13617). As of EF version `8.0.0`, QueryableValues remains superior in terms of compatibility and performance.

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
[DateOnly]: https://docs.microsoft.com/en-us/dotnet/api/system.dateonly
[TimeOnly]: https://docs.microsoft.com/en-us/dotnet/api/system.timeonly
[Guid]: https://docs.microsoft.com/en-us/dotnet/api/system.guid
[Char]: https://docs.microsoft.com/en-us/dotnet/api/system.char
[String]: https://docs.microsoft.com/en-us/dotnet/api/system.string
[Enum]: https://docs.microsoft.com/en-us/dotnet/api/system.enum
[BuyMeACoffee]: https://www.buymeacoffee.com/yv989c
[BuyMeACoffeeButton]: /docs/images/bmc-48.svg
[Repository]: https://github.com/yv989c/BlazarTech.QueryableValues

[benchmarks]: /benchmarks/QueryableValues.SqlServer.Benchmarks
[BenchmarksChart]: /docs/benchmarks/images/v8.1.0.png
[BenchmarksChartInteractive]: https://chartbenchmark.net/#shared=%7B%22v%22%3A1%2C%22settings%22%3A%7B%22display%22%3A%22Duration%22%2C%22scale%22%3A%22Log2%22%2C%22theme%22%3A%22Dark%22%7D%2C%22results%22%3A%22EIUwdgxgFgtghgJwNYBED2AXAciDACANwAYA6ARgGZyiAaPAdQEswATNAdwGc8yy8AKMqVIAmEQDYRZEiIDsZAKwB6MQAkRKomICqABxZwMIJQGUArmABqcADY2QATxEBKAFABBALIo8AJQcAXuB4AJx44iFERKomeADCCCCGaAh4AKIsjBiMaGB0fHEACtr54ng2aADmjBC2eHCseAAceLpQDpw1dRApIJyuJFhpACp4JigA0s0kpEJErnh4ANqqaJwYALqL2wBceIMj08ICTTMyVAoUQhTOdAAa4gAsfg5mAFIAkqPulnciC3g3mgAEYAWjSwHc7jeADE8HsDqNTsd%2BMjziRLtdbngHs9%2FO8vngfn9XK4TCAEAQKQBeYYIMwgRYfMAENC1bK5OJoCwYakiKJMowIQw5MBcnl8hSuXwWEwYYVGSoOameXJZFLMSqLbRgBBoOwwuAQDApal8BiIGBmXTisC8sikgA%2BeE8uCgaBY22dwwcukZi2dWDMMGBFIA8gAzaw2BncZ2uhrbJNJ51pBB61LJvDOuUsFAgAhZ%2BMgTKJrMBvwitDZysc8Y1gDi4CIXrwTbA5sb4BErfcdjZhhLNb7FQgtZy2dcjtBM9ns%2Bzc%2Fn08XK6Xq%2FXO2X65nm%2B3s93e7nB6PW8Xx5Pq%2FP%2B9PK6vl8dU4YWXdZnwNeZGAoPZr3%2FLyedSZkDQZDiLIJAUHgZhxkmjxUHIkHQdsChgbIsgITWgHAbBJBhFBGHSAKAGLMILZEdsoIYUmFFkYs1FJqcaETMAlHJgRpGPkwGBQHcMA2BWH5fhWv5%2Fq2WYKBI4HoWRZCPCQkhSYBIQYnweE0Ys4mkC0qnESQjwKCxwgQWpdHJiZVEYY8slNGUTEsUmCjkGUD7OpxUBvJwuTvnagk%2FiJ%2F7lrBIjkAp2wgRiaHadsYjkD2kXJo8chySFpAUGhZHCD2xl2XgZnkZRVySbZamLFQ4jscVolFn5VXVSmtXZeWFV%2FsVTUVW1zXVU1KYcc%2B3Jvs6Anfs6LT1TRQEhJcukhepsniPpcV4LIQUTdNizjQoSnPJFbEGTMpFZrltHZWZDGMcxFU7c5T5cTxfFeZ%2BQ3NPVDUbdIEELSISkUC2C0UEpvCrUhE1JZFKX7dsGXHVDWUAXpuk9kVf4OWQLRXa57meQN3mPSNtXFZZpARYha3SAoKnEzwDmpYDiywUpRMYaIRlJsIfAwwd0MldIqF4Ij5alZlj4ie1z0NcLo0S6LLXi41HWy35XVej1XF9fx2MVj5nXJiINDiJIMzTdc4GxcTlBgSE70U3gOviE0DEhZdLN7Zzpku6EMW8%2Bdf6Xcr3G8WrD0a8JMvJk00WWyxsiaVtFMhLJyE080sjiJJoPgWzTtEM87OuzneCPKcCNe%2BWDmfZOLnPhjYD3T5zqawrAVNNI5MsVIMwM2RIinJQiePLIDllGnKO7UQGe5xzOcUFQdue2LVBLeXDfy89iv%2BXjksr8vNXb2vO9b8LvuqzXj1kF369JjrjyRDIhtEFQVyG6lOEtzRl%2FX1piGOxDzt53lOeKOBM6YsfYVxuv7Y%2BFZT64xDmtGgcdpAxxYiEFOU9DZdwxLhK2QEmioVTohQmqNM76V%2FkdHOG0MQtD5lmaeqNfZVwgTWKB59kxlVkogsifdHKrX1sgxOkhZrJRkNnTOzNx5iPMkRR4pNZ4VSClHReMC6pS1FmLXeS9N57yUZois%2B9tFFkPq%2BAOtc8Bk2DvvHWChHgp0HqbT6ukO6hSWiQJo7DtY0AUKBDEDsf6Z3BhIieATFjRSARdH%2BvtbpGMeqY5hixxA0CkSneaFNyG21WrIJSoFAZxMeHfLxkUgqXF2mQNKgT%2FHj14EFEQ%2BkqHxRwsQuhHlq5Y0DjWaJWsswhCaPTR%2BpwyqGyaA5O%2BidIgpUEX9EeJTxF%2F0CbIAWMi%2FxwWzkLOWeiVkb3URsmJlU9Ei1WTozqBj%2Bp4EGkJLO0DzG6y%2FM4x%2BmkP6UWKbJBeC1raXOsd4wivi3bTLEXNGYfAamATCaAv2d1mnGP5AXdpQTHg0FSmBCORERAUBTjzOKVwU5EEwa3GFVTpB3IAl9PxeBSBPC%2BaQgJFArFyUocXMS4EjINMxsc9WP4zlQqCbrUeeTiZiGRg4kqUQ5I%2FStpfdJN804hDCOlcgoiylyumRC3S%2FzaXJioDJBRaytHrK2Zq3Vmz9k7L1aog1B9gVH2dA2MwjBPRCQ0VFGgRAFDIlWlcig%2BKYJwRNnZHWWcwJeoAl%2FHSHypnkvEacEIRdgFAuuiCislrrWtjMXqsmYEX6AVOE0BFF9kauLEgA%2FlohiGZzHvK0NZS4ZIvmSXdui90aNMbFam1vltW0xyQbBaYVkWrTbi43ubrgppykCPEt3zS05QwgVDxVbqEYiWV1XZLb9W6OXYok1Wr13bI3Wo01MbzVtkba2c5iiFA0CbtI36hN%2FUwQImm%2Byp6iAoveUSkiZLx3%2F1ksU6drFo2uQiQ2hNFYj3JtmQOimrrc20zVfy5MTrjhD2fX819h03202sc8AF2w4JSsZU0%2FdAGaxAeXVYvFq1ikkFRRTJF5Gr1ZisXB%2FBMxCHf2DWO5DJkrl6y%2FRfcC5Ul3Gr2QJtdK69WtSNQu4T3UzWGP%2FU2uuSad78lhegmjsSqBx1WgM8CNjW60EpbMJ9I8iVltHd8sjmauOhR%2Fc%2BP9FqD1B0Xahc9FNcGZLikbVJzzFiZu7oIrOw6kNu3EjhCzQScJpRwzJ1s9dV0mKqaBlip9dJZtCtY4VL025JMZjMWVxKfEhpQ2I5F4UQvWxmPU%2BdRrKsSc3VV7RomV3icE7W3q0nbP4edEw9lTRaAePntNCIDkrHTQjacBKGndP9rSx1vaCGWMmeM6GuGsESsgJjTZvDsmeBn3ZTwS5b1b7T2SzwftfTnlAQUB49t0EMqzaMwV1jGFcG8Iw2tajzWuL0La5tzrS7ZBtsy2ReQ5GsVkWBuZzzi1rLXO0iRKVnySH3e%2BUFogNKo2KA1Q1lRq8t1NexzV1RjWhP8ex2jFrRz42bbaeoqetAlokv61ShOcVrJqoB5h76p6CoOLYrdgL7NKCHZW1ZsBoKNutip8e4CmLuX3KpW6w2wMpGAxPY6x9MO%2FlFuY3dtjnMUayQtkL%2BXpOPv1q%2B%2BL0%2BWzzucKOzJZG2miJkAtuR91mEEoOQcSRTXQbtd854AAkIIS%2FyaXCxV6r26du1aJyJmLYtCc4%2F%2FIcuNdnWWQobkBWCOKrlXphSz522lskhGkLw7S6erjuKd%2B6nnhnWkzf2oMqIdfa%2BtJ4aj0JhFwngLN6c1PMDHfxLJvFwHA8Xc8C5UMjtIR4nYSm7l0ewite%2B%2FHuIJ%2B6qXs8Ble9typuxfd8I5uk9YgU4g6ijJZxM%2BglQ%2F7srrCoz1dRHn97xf%2FjUpBRwSVklTllmY%2BUT%2FiPYet31aGrVYk6J41hygICajNo6qhDJzkarT%2FZs5IQfoQZJgB7NwGbSpzYLaI5loZLVIqqArt7ArrbgGQHOjybaKWL6Ydo0KkanCFIQ6XC9KCIXYjw5bYE66wwORhwlaPLlYxqfZjDyhkHWx2r2RhTn5BLNxHbWyFy3owan7n6kCFKYEP7zY4EFZXKr4EGYb0oY5AFiHQG45Y7CZx5x7476JSZHKkFgBajDSGGXxNzBYLTYTy4LTITPyAyipKQu6Bqz4%2B4I4mSn7PY6GvZEFrad5CEQG2EEaGGxIo5wEXq6Qj75wkrH5ZjiCKGCJVIjyZSlLqEmScJjZr5Qab6CE2F2FPSLqZHwp0GMbdrIF8KUBXZZY5GYEjocEuxTxyQ2ShGlZtFf4GGLox7h7%2F746AGjGx5%2F6WG7qtZREiHRaapNAOoPrOEUyWTkYoHkKzKAzLGjz0b4R5aP6BEuz8jgRhBr6ra%2FqREVE1iLHLreYgwUyZoyDyG8AEQpHbCRApw0azCzYdEaGcFIRqYlZBSkrG5b5Mq3FyaLouL%2FTcKzTQbNAoIyHbBdJBRKHkAjqsxP7fJPwrSXHOKf6h5NYWGR78ZR7LoTE47mGUlKxWEVjQlbZ77bJl4v4y5kROrOIpGyC9KSF4BslhyJGfxHH%2BG4mhpBaF5C7hHXGi5Mk%2FaKLYJaDiqmxUrDxubAzqpna6zXwFozBOTFrikFZip%2FZC5kxlHb7ynbabLYLL4tEO7fRyQqYPLgSfFAT9wZq%2BYGnMYAlAliKXYbTSkh58Z450mTHhnx5Ukx5mHTEJ4MlgHCExHOgS4rIC7ASOlHbL4fqIHNCM7QZpkFzcwYHw75GdEda%2FLmaEkynWY3GJmVEpn7wnqF7SD26hSUrkA5nFJKQo7X44Kv6%2BZqEvonH87XwUD4Fo78F1pQl1mtIW47ZxKgTLSGy%2FJuE8pnG8lZKXKnDJbCBe5inDkBKUDSAiDoZ9GEzYYkkRmklhkUm3njHRliaxmSazHWEznkFsrqKXAnqgTdnDYubH5ATfTIzpEmJjmwq2xrGHFzZDkmJN7JlwWwUN7gy2xgSnlRrVki6MlvnWwfkwJiDASF6QVkS%2FLpIaZgTg6UY6woxfQDnV4HlL4XZyTjkVSkArQQnlHYUQosmtyyBwKTQ27dFOqGy8nGzdq8XIIFK%2BZ7kwX5ZAmBQyCB7lgkpGRAA%3D%3D%22%7D "Click for interactive chart"
[`OPENJSON`]: https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql