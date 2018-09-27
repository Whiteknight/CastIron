# CastIron

**CastIron** is an implementation of the *Query Object* and *Command Object* patterns to simplify data access in your application. You provide an object which encapsulates your query, and CastIron will create the connection, execute your query, and return to you the results.

CastIron is modular. You can use the parts you want and ignore the parts you don't. CastIron doesn't impose any required structure onto your program, nor require you to use any feature you don't want. CastIron *is not an ORM* like EntityFramework or NHibernate. It's closer to a Micro-ORM like Dapper.

Features of CastIron:

1. Write your own SQL instead of relying on an SQL code generate to produce messy, unoptimized code
1. Make use of all SQL features, instead of just the few features an ORM would expose to you
1. Easily encapsulate your commands and queries into objects, for easy reuse and adherance to the **Single Responsibility Pattern**
1. Easy batching of multiple commands and queries together
1. Mapping of result sets to values and objects

## Quick Start

Start by creating an `ISqlRunner`:

```csharp
var runner = RunnerFactory.Create(connectionString);
```

