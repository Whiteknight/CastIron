# Query Objects

There are several interfaces which can be used to designate and create a Query Object. In general, there are two basic types: `ISqlQuery` which is expected to create a result set and will be executed with `IDbCommand.ExecuteReader()` internally, and `ISqlCommand` which is not expected to return any result sets and will be executed with `IDbCommand.ExecuteNonQuery()`.

## Traditional Query Object Pattern

Martin Fowler, author of **Patterns of Enterprise Application Archiecture** [describes the query object pattern](https://martinfowler.com/eaaCatalog/queryObject.html) as:

    A Query Object is an interpreter, that is, a structure of objects that can form itself into a SQL query. You can create this query by refer-ring to classes and fields rather than tables and columns. In this way those who write the queries can do so independently of the database schema and changes to the schema can be localized in a single place.

This view seems limited to Query Object being only for result-returning queries and not for non-result commands. It also seems to require an explicit mapping portion to map from the application domain (object and property names) and the data domain (table and column names). 

CastIron takes a slightly different view of query objects. Instead, CastIron considers a Query Object to encapsulate a single database interaction and imposes no particular internal structure or usage to the object. A Query Object can contain a simple string of raw SQL code, or it could contain a `StringBuilder` to build SQL based on parameters, or it could contain a more complicated implementation of a **Visitor** or **Interpreter** to convert a series of **Criteria** objects to SQL as Mr. Fowler prescribes.

While it would be possible to build an explicit mapping layer from object names to table names, and property names to column names, and then use something like Expressions and LINQ queries to create SQL, CastIron only provides the most basic versions of these ideas, currently.

## Query Object Types and Tags

### `ISqlCommand`

A command on the database with no result sets or output parameters. The `SetupCommand()` method requires the manual setup of the `IDataInteraction` object, including setting the SQL text and any parameters which the command requires. `ISqlCommand` is executed with `IDbCommand.ExecuteNonQuery()` internally.

### `ISqlCommand<T>`

A command on the database which expects to return a result. The `SetupCommand()` method requires the manual setup of the `IDataInteraction` object, including setting the SQL text and any parameters which the command requires. The result may come from output parameters or other contextual information or metadata. `ISqlCommand` is executed with `IDbCommand.ExecuteNonQuery()` internally.

### `ISqlCommandSimple`

A simple command on the database with no result sets or output parameters. A raw string of SQL is returned from the `GetSql()` method. If parameters are required, they can be added using the `ISqlParameterized` interface (described below). This type expects to return no results. `ISqlCommandSimple` is executed with `IDbCommand.ExecuteNonQuery()` internally.

### `ISqlCommandSimple<T>`

A simple command on the database with no result sets or output parameters. A raw string of SQL is returned from the `GetSql()` method. If parameters are required, they can be added using the `ISqlParameterized` interface (described below). This type expects to return results, which may come from output parameters, contextual information or metadata. `ISqlCommandSimple` is executed with `IDbCommand.ExecuteNonQuery()` internally.

### `ISqlQuery`

A query on the database which is expected to return one more more result sets. This object uses the `SetupCommand()` method to manually setup the `IDataInteraction` object. Additional parameters may be added using the `ISqlParameterized` interface (described below). `ISqlQuery` is executed internally using the `IDbConnection.ExecuteReader()` method. 

This type does not have an explicit result type declared and will not return any results directly. This is used primarily for internal purposes. For most use-cases, consider using `ISqlQuery<T>`, described below.

### `ISqlQuery<T>`

A query on the database which is expected to return one more more result sets. This object uses the `SetupCommand()` method to manually setup the `IDataInteraction` object. Additional parameters may be added using the `ISqlParameterized` interface (described below). `ISqlQuery` is executed internally using the `IDbConnection.ExecuteReader()` method. 

### `ISqlQuerySimple`

A simple query on the database which provides a string of raw SQL and is expected to return one or more result sets. This object uses the `GetSql()` method to get a string of raw SQL to execute. Parameters, if required, may be added using the `ISqlParameterized` interface (described below). `ISqlQuerySimple` is executed internally using the `IDbConnection.ExecuteReader()` method.

This type does not have an explicit result type declared and will not return any results directly. This is used primarily for internal purposes. For most use-cases, consider using `ISqlQuerySimple<T>`, described below.

### `ISqlQuerySimple<T>`

A simple query on the database which provides a string of raw SQL and is expected to return one or more result sets. This object uses the `GetSql()` method to get a string of raw SQL to execute. Parameters, if required, may be added using the `ISqlParameterized` interface (described below). `ISqlQuerySimple` is executed internally using the `IDbConnection.ExecuteReader()` method.

### `ISqlConnectionAccessor`

This type provides direct access to the underlying `IDbConnection` and `IDbTransaction` objects, and puts no limitations or expectations on the usage of these objects. The operation is not expected to return any results. The `IDbConnection` object will be opened when provided to the accessor, and will be disposed by CastIron when the access has completed. The `IDbTransaction` object will be `null` if a transaction is not being used. This type is not expected to return any result objects.

This interface provides easy access to some of the low-level connection details which would not be exposed through any of the interfaces above, and is a good tool to help with migrating existing systems which use `System.Data` primitives directly to use CastIron instead.

### `ISqlConnectionAccessor<T>`

This type provides direct access to the underlying `IDbConnection` and `IDbTransaction` objects, and puts no limitations or expectations on the usage of these objects. The operation is not expected to return any results. The `IDbConnection` object will be opened when provided to the accessor, and will be disposed by CastIron when the access has completed. The `IDbTransaction` object will be `null` if a transaction is not being used. This type is expected to return some sort of a result, as from a result set, output parameters, connection metadata or other sources.

This interface provides easy access to some of the low-level connection details which would not be exposed through any of the interfaces above, and is a good tool to help with migrating existing systems which use `System.Data` primitives directly to use CastIron instead.

### `ISqlParameterized`

A tag type which can be used with any of the `ISqlCommand` or `ISqlQuery` variants above. The presence of this interface indicates that the query is parameterized separately from the `SetupCommand()` or `GetSql()` methods. This adds a second method call which can be executed to get additional parameters not already setup in the command or query.

### `ISqlStoredProc`

A tag type which can be used with any of the `ISqlCommand` or `ISqlQuery` variants above. The presence of this interface indicates that a Stored Procedure on the database will be executed and that the SQL query text is the name of the stored procedure instead of raw SQL to execute directly.

