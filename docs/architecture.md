# CastIron Architecture

CastIron is generally divided into two major parts: Execution and Mapping. The Execution portion consists of the `ISqlRunner`, the various query object interfaces and batching logic. The Mapping portion consists largely of the `IDataResults` object and the `IMapCompiler` suite. You can use one part without being forced to use the other.

## Design Goals

CastIron has a number of design goals:

1. Pluggability. CastIron should provide sane and powerful defaults, but everything should be pluggable for cases where different behaviors and features are required.
1. Targetted Performance. Common operations should be fast, even if uncommon operations may be slow.
1. Unobtrusiveness. CastIron should never get in the way and should never impose design decisions on your software. You should be able to fall back to trusty `System.Data` objects and methods at any time.
1. Honesty. Mapping between an SQL database and the rich type system of a programming language like C# is a hard problem with many difficulties. ORMs try to hide these difficulties, but CastIron wants to be honest about what it can and cannot do.
1. Query Object Pattern. CastIron suggests, but does not require, the use of the Query Object Pattern to help organize your code and encapsulate your database interactions.
1. Simplicity. CastIron is not an ORM and does not aspire to be one. Building schemas, tracking schemas and maintaining object mappings are all strictly outside the purview of CastIron.
1. Helpfulness. CastIron provides discoverable and fluent interfaces, and helpful error messages to assist developers.

## Important Abstractions

The basic types provided in `System.Data` have limitations and also can be difficult to use cleanly. Additionally, different providers often have different capabilities which can make writing code for multiple providers unnecessarily difficult. Here is a small sample of the basic limitations and drawbacks which CastIron aims to smooth over:

* `IDbConnection` and `IDbCommand` interfaces don't provide any async method variants, even though most concrete provider types do provide these.
* Adding parameters to `IDbCommand` can be very verbose, especially if you want to share code between providers. Different providers handle these parameters differently.
* Output parameters are exposed as `object` from the `IDbCommand` but `IDataReader` provides type-safe access methods for result set data.
* `IDataReader` will return `DbNull` instead of `null` values and will throw unhelpful exceptions instead of casting to `null` or a default value when a value is accessed.

For these and several other reasons, CastIron provides several wrappers and abstractions which help to shield the developer from these problems:

### `IDbConnectionAsync` and `IDbCommandAsync`

These types are mostly for internal use, and expose async method variants for `IDbConnection` and `IDbCommand` interfaces, respectively. The underlying `System.Data` objects can be retrieved from `IDbConnectionAsync.Connection` and `IDbCommandAsync.Command` respectively.

### `IDataInteraction`

The `IDataInteraction` is a wrapper around `IDbCommand` (and `IDbCommandAsync`) which allows for easy, type-safe setup of parameters and SQL text. The underlying `IDbCommand` object can be accessed directly from `IDataInteraction.Command` if required.

### `IDataResults`

The `IDataResults` object wraps the `IDataReader` and `IDbCommand` objects to provide access to all result sets and output parameters. To get the raw `IDataReader` object, call `IDataResults.AsRawReader()`. If you want a raw reader object which uses better error messages (at a slight performance penalty) call `IDataResults.AsRawReaderWithBetterErrorMessages()`.

This type is the gateway to accessing the [Mapping Subsystem](mapping.md).

## Execution Basics

When you pass an `ISqlQuery` variant to `ISqlRunner.Query()` or an `ISqlCommand` variant to `ISqlRunner.Execute()`, the runner will open a new `SqlConnection`, initialize an `SqlCommand` with details from the query object, and execute the command on the database. When the command has been executed, the `IDataReader` is wrapped up as an `IDataResults` object for mapping.

See [Query Objects](queryobjects.md) for more details.

## Mapping Basics

When the `ISqlRunner` executes the command on the database, the results are wrapped up as an `IDataResults` object. This object provides access to the results from the `IDataReader` as well as any output parameters from the command and other helpful metadata.

See the page on [Result Mapping](mapping.md) for more details about the algorithms and heuristics used to map columns from the result set into objects.