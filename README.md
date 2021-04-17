# CastIron

A micro-ORM with the Query Object Pattern for C#

## Overview

CastIron is a micro-ORM which focuses on power, flexibility, modularity, usability, and the **Query Object** pattern. These are the central design goals, the relative prioritization of which separates it from other Micro-ORMs. 

CastIron is written in a provider-agnostic way using abstractions from `System.Data` instead of concrete classes. The CastIron project offers providers for multiple database types:

1. **CastIron.Sql** targets Microsoft SQL Server
1. **CastIron.Sqlite** targets SQLite. 
1. **CastIron.Postgres** targets PostgreSQL

## Get Started

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
public class MyTestQuery : ISqlQuerySimple<MyObject>
{
    public string GetSql() {
        return "SELECT TOP 1 Col1, Col2 FROM dbo.MyTable;";
    }

    public MyObject Read(IDataResults result) {
        return result.AsEnumerable<MyObject>().SingleOrDefault();
    }
}
```

And it's use:

```csharp
var runner = RunnerFactory.Create(connectionString);
var result = runner.Query(new MyTestQuery());
```

The `IDataResults` object contains the raw `IDataReader` object, any output parameters, and utilities for automatically mapping rows to objects and values. Experimenting with the possibilities of `IDataReader` is key to finding happiness and success with CastIron.

Please see the existing unit tests for additional, in-depth examples of using CastIron.Sql.

## Status

CastIron.Sql is stable, in active development, and used in production deployments. v2.0.0 is released to nuget and is preferred over all previous versions.

Providers are available for SQL Server, SQLite and PostgreSQL. Providers for other databases such as MySql are in development.
