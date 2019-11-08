# CastIron Architecture

CastIron is generally divided into two major parts: Execution and Mapping. The Execution portion consists of the `ISqlRunner`, the various query object interfaces, batching logic and execution strategies. The Mapping portion consists largely of the `IDataResults` and `IDataResultsStream` objects and the `IMapCompiler` suite. You can use one part without being forced to use the other, and both sections of CastIron provide many options and opportunities for pluggability to help customize the library to your work flow.

## Design Goals

CastIron has a number of design goals:

1. **Pluggability**. CastIron should provide sane and powerful defaults, but everything should be pluggable for cases where different behaviors and features are required.
1. **Targetted Performance**. Common operations should be fast, even if uncommon operations may be slow.
1. **Unobtrusiveness**. CastIron should never get in the way and should never impose design decisions on your software. You should be able to fall back to trusty `System.Data` objects and methods at any time.
1. **Honesty**. Mapping between an SQL database and the rich type system of a programming language like C# is a hard problem with many difficulties. ORMs try to hide these difficulties, but CastIron wants to be honest about what it can and cannot do.
1. **Query Object Pattern**. CastIron suggests, but does not require, the use of the Query Object Pattern to help organize your code and encapsulate your database interactions as reusable components.
1. **Simplicity**. CastIron is not an ORM and does not aspire to be one. Building schemas, tracking changes and maintaining object mappings are all strictly outside the purview of CastIron.
1. **Helpfulness**. CastIron provides discoverable and fluent interfaces, and helpful error messages to assist developers.

## Important Abstractions

The basic types provided in `System.Data` have limitations and also can be difficult to use cleanly. Additionally, different providers often have different capabilities which can make writing code for multiple providers unnecessarily difficult. Here is a small sample of the basic limitations and drawbacks which CastIron aims to smooth over:

* `IDbConnection`, `IDbCommand` and `IDataReader` interfaces don't provide any `async` method variants, even though most concrete provider types do provide these.
* Adding parameters to `IDbCommand` can be very verbose, especially if you want to share code between providers. Different providers handle these parameters differently.
* Output parameters are exposed as `object` from the `IDbCommand` but `IDataReader` provides type-safe access methods for result set data.
* `IDataReader` will return `DBNull` instead of `null` values and will throw unhelpful exceptions instead of casting to `null` or a default value when a value is accessed.
* `SqlException` and its variants will tell you that there is an error, but won't tell you what query was being executed or what parameters were passed, forcing you to recreate this information manually.

For these and several other reasons, CastIron provides several wrappers and abstractions which help to shield the developer from these problems:

### `IDbConnectionAsync`, `IDbCommandAsync` and `IDataReaderAsync`

These types are mostly for internal use, and expose async method variants for `IDbConnection`, `IDbCommand` and `IDataReader` interfaces, respectively. The underlying `System.Data` objects can be retrieved from `IDbConnectionAsync.Connection`, `IDbCommandAsync.Command` and `IDataReader.Reader` respectively.

`IDbConnectionAsync` and `IDbCommandAsync` are used to expose Async method variants with Tasks in the `ISqlRunner`. `IDataReaderAsync` is only used to provide an `IAsyncEnumerable<T>` interface, which is only available in the .NET Standard 2.1 build but not in the .NET Framework 4.5 or .NET Standard 2.0 builds. If your application does not target .NET Standard 2.1, .NET Core 3.0 or higher, you won't make use of `IDataReaderAsync` or `IAsyncEnumerable<T>`.

### `IDataInteraction`

The `IDataInteraction` is a wrapper around `IDbCommand` (and `IDbCommandAsync`) which allows for easy, type-safe setup of parameters and SQL text. The underlying `IDbCommand` object can be accessed directly from `IDataInteraction.Command` if required.

### `IDataResults`

The `IDataResults` object wraps the `IDataReader` and `IDbCommand` objects to provide access to all result sets and output parameters. To get the raw `IDataReader` object, call `IDataResults.AsRawReader()`. If you want a raw reader object which uses better error messages (at a slight performance penalty) call `IDataResults.AsRawReaderWithBetterErrorMessages()`.

This type is the gateway to accessing the [Mapping Subsystem](mapping.md).

### `SqlQueryException`

CastIron provides `SqlQueryException` which includes the `SqlException`, the query text and the parameter values for easy debugging.

## Execution Basics

When you pass an `ISqlQuery` variant to `ISqlRunner.Query()` or an `ISqlCommand` variant to `ISqlRunner.Execute()`, the runner will open a new `SqlConnection`, initialize an `SqlCommand` with details from the query object, and execute the command on the database. When the command has been executed, the `IDataReader` (if you're executing a query) is wrapped up as an `IDataResults` object for mapping.

See [Query Objects](queryobjects.md) for more details.

## Mapping Basics

When the `ISqlRunner` executes the command on the database, the results are wrapped up as an `IDataResults` object. This object provides access to the results from the `IDataReader` as well as any output parameters from the command and other helpful metadata.

See the page on [Result Mapping](mapping.md) for more details about the algorithms and heuristics used to map columns from the result set into objects.

You can provide your own mappings, if you have legacy code around which already accesses `IDataReader` to get the results you wish. However, an important feature of CastIron is the ability to automatically compile a mapping function from a result set to an enumerable of result objects. The map compiler is a Composite object consisting of three parts:

1. `IScalarCompiler` which compiles a transformation from a single column to one of the primitive types.
1. `ICompiler` which compiles transformations from several columns to an array, collection, dictionary, tuple or other object
1. `IMapCompiler` which manages the `ICompiler`s and combines the output into a `Func<IDataRecord, T>` which can be executed on every row of the result set.