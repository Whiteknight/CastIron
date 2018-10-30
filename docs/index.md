# CastIron

**CastIron** is an implementation of the *Query Object* pattern to simplify data access in your application. You provide an object which encapsulates your query, and CastIron will create the connection, execute, and return the results.

CastIron is modular. You can use the parts you want and ignore the parts you don't. CastIron doesn't impose any required structure onto your program, nor require you to use any feature you don't want. CastIron *is not an ORM* like EntityFramework or NHibernate. It's closer to a Micro-ORM like Dapper, but with the structure of the Query Object pattern.

Features of CastIron:

1. Write your own SQL instead of relying on an SQL code generator to produce messy, unreadable, unoptimized code
1. Make use of all SQL features, instead of just the few features an ORM would expose to you
1. Easily encapsulate your commands and queries into objects, for easy reuse and adherance to the **Single Responsibility Pattern**
1. Easy batching of multiple commands and queries together
1. Automatic and configurable mapping of result sets to values and objects

## ISqlQuery and ISqlCommand

The `ISqlQuery` type and related types (`ISqlQuery<T>`, `ISqlQuerySimple`, `ISqlQuerySimple<T>`) encapsulate a call to `IDbCommand.ExecuteReader()`, which produces an `IDataReader` for reading results. These results may also have values from output parameters, if any were provided.

The `ISqlCommand` type and its variants (`ISqlCommand<T>`, `ISqlCommandSimple`, `ISqlCommandSimple<T>`) encapsulate a call to `IDbCommand.ExecuteNonQuery()` which will not produce an `IDataReader` and expects not to have explicit result sets. The results from these may be calculated or they may be derived from output parameters (if any are provided).

## Quick Start

Start by creating an `ISqlRunner`:

```csharp
var runner = RunnerFactory.Create(connectionString);
```

## I want to...

### Execute a query object and get a mapped result

```csharp
var result = runner.Query(new MyQueryObject());
```

### Execute a command object

```csharp
runner.Execute(new MyCommandObject());
```

### Execute a query object and get a raw `IDataReader`

```csharp
var reader = runner.QueryStream(new MyQueryObject()).AsRawReader();
```

### Execute a string of SQL and get a mapped result

```csharp
var result = runner.Query("SELECT * FROM MyTable");
```

### Execute a string of SQL which does not return a result

```csharp
runner.Execute("UPDATE MyTable SET ...");
```

### Just execute an SQL query and get an `IDataReader`

```csharp 
var result = runner.QueryStream("SELECT * FROM MyTable").AsRawReader();
```

### Map an existing `IDataReader` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(reader).AsEnumerable<MyType>();
```
### Map an existing `DataTable` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(table).AsEnumerable<MyType>();
```


