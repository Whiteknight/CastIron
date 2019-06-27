# Quickstart

Start by creating an `ISqlRunner` for your chosen provider:

```csharp
// MS SQL Server, requires CastIron.Sql package
var runner = CastIron.Sql.RunnerFactory.Create(connectionString);

// SQLite, requires CastIron.Sqlite package
var runner = CastIron.Sqlite.RunnerFactory.Create(connectionString);

// PostgreSQL, requires CastIron.Postgres package
var runner = CastIron.Postgres.RunnerFactory.Create(connectionString);
```

## I want to...

### Execute a query object and get a mapped result

```csharp
var result = runner.Query(new MyQueryObject());
```

```csharp
var result = runner.Query(new MyQueryObject(), new MyResultMaterializer());
```

```csharp
var result = runner.Query(new MyQueryObject(), r => r.AsEnumerable<MyResultType>().ToList());
```

```csharp
// All the above methods also have Async variants
var result = await runner.QueryAsync(new MyQueryObject());
```

### Execute a command object

```csharp
runner.Execute(new MyCommandObject());
```

```csharp
await runner.ExecuteAsync(new MyCommandObject());
```

### Execute a query and stream results

```csharp
using (var stream = runner.QueryStream(new MyQueryObject())) {
    foreach (var result in stream.AsEnumerable<MyResultType>()) {
        ...
    }
}
```

```csharp
using (var stream = runner.QueryStream("SELECT * FROM MyTable")) {
    foreach (var result in stream.AsEnumerable<MyResultType>()) {
        ...
    }
}
```

```csharp
using (var stream = await runner.QueryStreamAsync(new MyQueryObject())) {
    foreach (var result in stream.AsEnumerable<MyResultType>()) {
        ...
    }
}
```

### Execute a query and get a raw `IDataReader`

```csharp
using (var stream = runner.QueryStream(new MyQueryObject()))) {
    using (var reader = stream.AsRawReader()) {
        ...
    }
}
```

```csharp
using (var stream = runner.QueryStream("SELECT * FROM MyTable"))
{
    var reader = stream.AsRawReader();
    ...
}
```

### Execute a string of SQL and get a mapped result

```csharp
var result = runner.Query<MyResultType>("SELECT * FROM MyTable");
```

```csharp
var result = runner.Query("SELECT * FROM MyTable", new MyResultMaterializer());
```

```csharp
var result = runner.Query("SELECT * FROM MyTable", r => r.AsEnumerable<MyResultType>().FirstOrDefault());
```

### Execute a string of SQL which does not return a result

```csharp
runner.Execute("UPDATE MyTable SET ...");
```

### Map an existing `IDataReader` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(reader).AsEnumerable<MyType>();
```

### Map an existing `DataTable` to an enumerable of objects

```csharp
var result = runner.WrapAsResultStream(table).AsEnumerable<MyType>();
```

### Batch multiple queries and commands onto a single connection and execute at once

```csharp
var batch = runner.CreateBatch();
var promise1 = batch.Add(new MyQueryObject());
var promise2 = batch.Add(new MyQueryObject(), new MyResultMaterializer());
var promise3 = batch.Add<MyResultType>("SELECT * FROM MyTable");
batch.Add(new MyCommandObject());

runner.Execute(batch);

var result1 = promise1.GetValue();
var result2 = promise2.GetValue();
var result3 = promise3.GetValue();
```
