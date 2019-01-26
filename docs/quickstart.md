# Quickstart

Start by creating an `ISqlRunner` for your chosen provider:

```csharp
// MS SQL Server, requires CastIron.Sql package
var runner = CastIron.Sql.RunnerFactory.Create(connectionString);

// SQLite, requires CastIron.Sqlite package
var runner = CastIron.Sqlite.RunnerFactory.Create(connectionString);
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
using (var stream = runner.QueryStream(new MyQueryObject())))
{
    var reader = stream.AsRawReader();
    ...
}
```

### Execute a string of SQL and get a mapped result

```csharp
var result = runner.Query<MyResultType>("SELECT * FROM MyTable");
```

### Execute a string of SQL which does not return a result

```csharp
runner.Execute("UPDATE MyTable SET ...");
```

### Just execute an SQL query and get an `IDataReader`

```csharp
using (var stream = runner.QueryStream("SELECT * FROM MyTable"))
{
    var reader = stream.AsRawReader();
    ...
}
```

### Map an existing `IDataReader` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(reader).AsEnumerable<MyType>();
```

### Map an existing `DataTable` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(table).AsEnumerable<MyType>();
```