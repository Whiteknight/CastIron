# CastIron

**CastIron** is a Micro ORM with particular focus on three ideas:

1. The *Query Object* pattern to encapsulate database accesses as reusable objects,
1. Deep modularity where you can use the parts you want and ignore or replace everything, and
1. Developer centricity with discoverable interfaces, easy fallbacks to tried-and-true `System.Data`, and helpful error messages.

CastIron doesn't impose any required structure onto your program, nor require you to use any feature you don't want. It is opinionated, however, and intends to encourage good software design.

Features of CastIron:

1. Write your own SQL instead of relying on an SQL code generator to produce messy, unreadable, unoptimized code
1. Make use of all SQL features, instead of just the few features an ORM would expose to you
1. Easily encapsulate your commands and queries into objects for easy reuse and adherance to the *Single Responsibility Principle*
1. Easy batching of multiple commands and queries together onto a single open connection
1. Automatic and configurable mapping of result sets to values and objects

See the [Quick Start](quickstart.md) guide to get started with some common usage patterns.

See the [Architecture Overview](architecture.md) page to get an idea of how CastIron works and the project design goals.

## `ISqlQuery` and `ISqlCommand`

The `ISqlQuery` type and related types (`ISqlQuery<T>`, `ISqlQuerySimple`, `ISqlQuerySimple<T>`) encapsulate a call to `IDbCommand.ExecuteReader()`, which produces an `IDataReader` for reading results. These results may also have values from output parameters, if any were provided.

The `ISqlCommand` type and its variants (`ISqlCommand<T>`, `ISqlCommandSimple`, `ISqlCommandSimple<T>`) encapsulate a call to `IDbCommand.ExecuteNonQuery()` which will not produce an `IDataReader` and expects not to have explicit result sets. The results from these may be calculated or they may be derived from output parameters (if any are provided).

See the [Query Objects](queryobjects.md) page for more details about how to encapsulate your database interactions into Query Objects.

See the [Result Mapping](mapping.md) and [Mapping Complex Objects](maponto.md) pages for more details about how result sets can be mapped to enumerables of values or objects.

[FAQ](faq.md)
