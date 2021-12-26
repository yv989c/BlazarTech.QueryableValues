<p align="center">
    <img src="https://raw.githubusercontent.com/yv989c/BlazarTech.QueryableValues/develop/docs/images/icon.png" alt="Logo" style="width: 80px;">
</p>

# QueryableValues [![CI/CD Workflow](https://github.com/yv989c/BlazarTech.QueryableValues/actions/workflows/ci-workflow.yml/badge.svg)](https://github.com/yv989c/BlazarTech.QueryableValues/actions/workflows/ci-workflow.yml)
This library allows us to efficiently compose an [IEnumerable\<T\>] in our [Entity Framework Core] queries when using the [SQL Server Database Provider]. This is done by using the `AsQueryableValues` extension method that is made available on the [DbContext] class. Everything is evaluated on the server with a single roundtrip, in a way that preserves the query's [execution plan], even when the values behind the [IEnumerable\<T\>] are changed on subsequent executions.

The supported types for `T` are:
- Simple Types: [Int32], [Int64], [Decimal], [Double], [DateTime], [DateTimeOffset], [Guid], and [String].
- Complex Type:
  - Can be an anonymous type.
  - Can be a user defined class or struct, with read/write properties and a public constructor.
  - Must have one or more simple type properties.

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

Below are two patterns that you can use to retrieve data from an entity based on values provided by an [IEnumerable\<T\>].

#### Simple Type, using the [Contains][ContainsQueryable] LINQ method:

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
#### Simple Type, using the [Join] LINQ method:
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
#### Complex Type, using the [Join] LINQ method:
```c#
// todo
```
**When providing a Complex Type**
> :warning: All the data provided by this type is transmitted to the server, therefore, ensure that it only contains the properties that you are using in your query. Not following this recommendation will degrade the query's performance.

> :warning: There is a limit of up to ten properties for any given simple type (e.g., cannot have more than ten [Int32] properties). Reaching that limit will cause an exception, and may also be a sign that you are doing too much.
---

## Background 📚
When [Entity Framework Core] is set up to use the [SQL Server Database Provider], and it detects the use of variables in a query, in *most cases* it provides their values as parameters to an internal [SqlCommand] object that will end up executing the translated SQL statement. This is done efficiently by using the [sp_executesql] stored procedure behind the scenes, so if the same SQL statement is executed a second time, our SQL Server instance will likely have a computed [execution plan] in its cache, therefore, saving time and system resources.

## The Problem 🤔
We have been in the situation where we need to build a query that must return one or more items based on a sequence of values. The common pattern to do this makes use of the [Contains][ContainsEnumerable] LINQ extension method on the [IEnumerable\<T\>] interface, and then we pass the property of the entity that must match any of the values in the sequence. This way we can retrieve multiple items with a single roundtrip to the database, as shown in the following example:

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
The previous query will yield the expected results, but there's a catch. If the sequence of values in our list *is different* on every execution, the underline SQL query will be built in a way that's not optimal for SQL Server's query engine. Wasting system resources like CPU, memory, IO, and potentially affecting other queries in the instance.

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
Here we can observe that the values in our list are being hardcoded as part of the SQL statement provided to [sp_executesql], as opposed to them being injected via a parameter, as is the case for our other variable holding the value 100.

Now, let's add another item to the list of values and execute the query again:
```tsql
exec sp_executesql N'SELECT [m].[MyEntityID], [m].[PropA]
FROM [dbo].[MyEntity] AS [m]
WHERE [m].[MyEntityID] IN (1, 2, 3, 4) OR ([m].[PropB] = @__p_1)',N'@__p_1 bigint',@__p_1=100
```
As we can see, a new SQL statement was generated just because we modified the list that's being used in our `Where` predicate. This has the detrimental effect that a previously cached execution plan cannot be reused, forcing SQL Server's query engine to compute a new [execution plan] *every time* it is provided with a SQL statement that it hasn't seen before, and increasing the likelihood of flushing other plans in the process.

## Enter AsQueryableValues 🙌
![Parameterize All the Things](/docs/images/parameterize-all-the-things.jpg)

This library provides the `AsQueryableValues` extension method, made available on the [DbContext] class. It solves the problem explained above by allowing us to build a query that will generate a SQL statement for [sp_executesql] that will remain constant, execution after execution, allowing SQL Server to do its best every time by using a previously cached [execution plan]. This will speed up our query on subsequent executions, and conserve system resources.

Let’s take a look at the following query making use of this method, which is functionally equivalent to the previous examples:
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
Great! The SQL statement provided to [sp_executesql] remains constant. In this case SQL Server can reuse the [execution plan] from our previous execution.

## The Numbers 📊
You don’t have to take my word for granted! Let’s see a trace of what’s going on under the hood when both of these queries are executed multiple times, adding a new value to our list after each execution. First, five (5) executions of the one making direct use of the [Contains][ContainsEnumerable] LINQ method (orange), and then five (5) executions of the second one making use of the `AsQueryableValues` extension method on our [DbContext] (green):

![Trace](/docs/images/as-queryable-trace.png)
<sup>Queries executed against SQL Server 2017 Express (14.0.2037) running on a resource constrained laptop.</sup>

As expected, none of the queries in the orange section hit the cache, on the other hand, after the first query in the green section, all the subsequent ones hit the cache and consumed fewer resources.

Now, let's focus our attention to the first query of the green section. We can appreciate that there's a cost associated with this technique, but this cost can be offset in the long run, especially when our queries are not trivial like the ones that I am using in these examples.

## What Makes this Work? 🤓
I am making use of the XML parsing capabilities in SQL Server, which are available in all the supported versions of SQL Server to date. The provided values are serialized as XML and embedded in the underline SQL query using a native XML parameter, then I use SQL Server's XML type methods to project the query in a way that can be mapped by Entity Framework.

This is a technique that I have not seen being used by other popular libraries that aim to solve this problem. It is superior from a latency standpoint because it resolves the query with a single roundtrip to the database, and most importantly, it preserves the query’s [execution plan], even when the content of the XML is changed.

## One More Thing 👀
The `AsQueryableValues` extension method allows us to treat our sequence of values as we normally would if these were another entity in the [DbContext]. The type returned by the extension is a [IQueryable\<T\>] that can be composed with other entities in your query.

For example, we can do one or more joins like this and it’s totally fine:
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
[IEnumerable\<T\>]: https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable
[IQueryable\<T\>]: https://docs.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1
[NuGet Package]: https://www.nuget.org/packages/BlazarTech.QueryableValues.SqlServer/
[readme-background]: #background-
[execution plan]: https://docs.microsoft.com/en-us/sql/relational-databases/query-processing-architecture-guide?#execution-plan-caching-and-reuse
[Int32]: https://docs.microsoft.com/en-us/dotnet/api/system.int32
[Int64]: https://docs.microsoft.com/en-us/dotnet/api/system.int64
[Decimal]: https://docs.microsoft.com/en-us/dotnet/api/system.decimal
[Double]: https://docs.microsoft.com/en-us/dotnet/api/system.double
[DateTime]: https://docs.microsoft.com/en-us/dotnet/api/system.datetime
[DateTimeOffset]: https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset
[Guid]: https://docs.microsoft.com/en-us/dotnet/api/system.guid
[String]: https://docs.microsoft.com/en-us/dotnet/api/system.string
