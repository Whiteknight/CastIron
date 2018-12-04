# Quickstart

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