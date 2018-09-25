# CastIron

Query Object and Command Object patterns for C#

## Overview

the **Query Object** pattern allows you to wrap a single database query into a reusable, parameterizable class with a descriptive name. An extension of this idea, the **Command Object** pattern, allows you to wrap database modifications (inserts, updates and deletes) into a reusable, parameterizable class with a descriptive name. I believe that this pattern can be a powerful tool and even superior to other data access patterns such as Active Record or Repository.

## CastIron.Sql

**CastIron.Sql** is a bare-metal implementation of the Query Object and Command Object patterns for SQL, specifically Microsoft SQL Server (it is usable, though lightly tested, with other providers such as MySQL and MariaDB). CastIron.Sql allows you to write SQL directly, using all the SQL features at your disposal, and possibly map the result sets to objects in your application. The intention is modularity and minimalism: Any feature you do not want to use is optional.

### Get Started

Create an implementation of either `ISqlQuery` or `ISqlCommand` with your SQL code and query reading logic. Pass this object to an instance of `SqlRunner`. The runner will do all the dirty stuff like opening the `SqlConnection`, creating the `SqlCommand` and obtaining an `SqlDataReader`. CastIron gives you all the tools to work with things like In/InOut parameters or multiple result sets. CastIron also provides tools to automatically map result sets to objects and `IEnumerable`s of objects. You can use these features if you want them, and ignore them if you don't. 

### Goals of CastIron and CastIron.Sql

CastIron currently has a few principles and goals:

1. Use the QueryObject and CommandObject patterns for working with the DB, as opposed to the Repository, Active Record, Unit Of Work, Table Data Gateway or other data access patterns.
1. Allow the programmer to write SQL without interference or imposed limitations
1. Do not do anything fancy like caching, but enable the programmer to add those features later if desired
1. Provide some basic tools and helpers, but allow the programmer to easily opt-out of anything which isn't needed.

## Examples

Start by creating an `ISqlRunner`:

```csharp
var runner = RunnerFactory.Create(connectionString);
```

At this point you will need to create a class to hold your SQL queries or command. Implement one of the following interfaces:

1. `ISqlQuery<T>` for a simple string of SQL which returns result sets
1. `ISqlQueryRawCommand<T>` you get the raw `IDbCommand` to which you can add query text and parameters, and get back query results.
1. `ISqlQueryRawConnection<T>` you get the raw `IDbConnection` on which you can do any operation you want
1. `ISqlCommand` for a simple string of SQL which does not return a result set
1. `ISqlCommandRaw` you get the raw `IDbCommand` to which you can add command text and parameters. This will return no results.
1. `ISqlCommandRaw<T>` like `ISqlCommandRaw` except this class will have an opportunity to read query results and output parameters.

As an example of a simple query:

```csharp
public class MyTestQuery : ISqlQuery<MyObject>
{
    public string GetSql() {
        return "SELECT TOP 1 Col1, Col2 FROM dbo.MyTable;";
    }

    public MyObject Read(SqlResultSet result) {
        return result.AsEnumerable<MyObject>().SingleOrDefault();
    }
}
```

And it's use:

```csharp
var runner = RunnerFactory.Create(connectionString);
var result = runner.Query(new MyTestQuery());
```

The `SqlResultSet` object contains the raw IDataReader accessible through the `.AsRawReader()` method, but also contains some utilities for automatically mapping rows to objects using property name <-> column name mappings, including multiple result sets. This object also exposes the `Output` and `InputOutput` parameter values to read those in a safe manner. Experimenting with the possibilities of `SqlResultSet` is key to finding happiness and success with CastIron.

Please see the existing unit tests for additional, in-depth examples of using CastIron.Sql.

## Limitations

Currently CastIron completely controls the lifecycle of the `IDbConnection`, which means the connection is closed when the `Query` or `Execute` method returns. Streaming results from these methods over an open connection is not currently supported.

CastIron mapping of result sets to objects is still in early stages of development and may be missing features of other micro-ORM tools.

CastIron requires a lot of new unit test coverage and real-world testing.

Providers for other databases such as MySql, SQLite and PostgreSQL are all in development.

## Status

CastIron.Sql is currently very early in development and is adding features at a rapid pace.

## FAQ

**Should Queries and Commands be wrapped up into stored procedures for better performance?**

Stored procs can be an optimization, but not necesarily a huge one. What you save with a stored proc is generally the cost of repeatedly parsing the SQL syntax and compiling it into an execution plan. There are a few problems with stored procs: 

1. Since they live in the DB, it can be very difficult to get them into version control and keep them synchronized with the application code.
1. Many organizations also demand extensive boiler-plate templates for stored procs, which can wipe out any performance gains (sometimes there are good reasons for this, often times it's just cargo-cult mentality nonsense). 
1. Cached execution plans may be suboptimal and will require manual intervention to recompile

Stored Procs can be an optimization, but for most work they are a premature optimization.

CastIron easily supports calling stored procs from your query and command classes, so it should be an easy transition to prototype the raw SQL in a query and then move that to a stored proc call later as required. It's probably worthwhile to do some benchmarking to make sure that the change of query to stored proc actually does lead to performance benefits, and not just maintainability headaches.

**Can I use CastIron to wrap existing data accesses such as Dapper?**

Yes. CastIron provides a `ISqlQueryRawConnection` interface which gives access to the raw underlying `IDbConnection` object, which Dapper or other micro-ORMs can operate on.



