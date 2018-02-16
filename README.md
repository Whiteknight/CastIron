# CastIron

QueryObject and CommandObject patterns for bare-metal SQL

## Rant

I used to be in love with ORMs like EF and NHibernate, but I've come to realize that all the complexities and abstractions they put in place start to cause more harm than benefit. The fundamental problem is the *impediance mismatch* between SQL and object-oriented programming languages like C#. The two domains just don't have the same basic building blocks and don't always make it easy to map from one to the other. ORMs try to bridge the gap with all sorts of fancy patterns and algorithms, but what they end up doing is making a lot of bold assumptions and then hiding all the advanced features of each domain which don't follow those assumptions. When you, the programmer, don't respect these limitations closely enough, you end up with incomprehensible error messages and a serious maintenance headache. Here are some assumptions that ORMs tend to make:

1. **Identity**: Objects map to rows in a table 1:1. One object can be stored in exactly one row, and one row can be materialized into exactly one object.
1. **Symmetry**: The model that you want to insert/update into the DB is the same model that you eventually want to query back from the DB. There are never multiple representations of the same underlying data. CQRS doesn't exist.
1. **Obscurity** The programmer never wants to see the SQL code generated by an ORM query, shouldn't be able to read it if they do see it, and absolutely shouldn't be able to tweak the SQL to make it perform better.
1. **Application Primacy**: We shouldn't be doing work on the DB, but instead should load entities out of the DB, modify them in the application, and save the changes back in a separate connection.

There are other bad assumptions made by ORMs, but these are the ones that were biting me. For comparison, here are the things that I wanted from a DB access tool:

1. I know how to write SQL, and I recognize that some problems can be well-solved using features like temporary tables, CTEs, stored procs, sequences, merges, functions, pivoting, etc.
1. I would like to be able to use the QueryObject pattern to encapsulate my querues as objects.
1. I would like to be able to use multiple result sets in my queries, when they have better characteristics than a single result set with many joins.

The disconnect between what modern ORMs offer and what I wanted is what eventually lead me to write CastIron.

## Alternatives

NoSQL Databases such as Document databases and Object databases often are much more closely aligned to the needs of OOP softare than relational databases are. If you are using a document database, many of the problems I mention above evaporate away completely. You may not need any abstraction or, if you are cautious, only a simple abstraction to isolate the dependency. From RavenDB, MongoDB, CosmosDB and a whole host of others, there is probably a good document database for you, depending on your organization, work load and cost considerations.

Various micro-ORMs such as Dapper and Simple.Data don't make the same assumptions as their more macro- competitors. But, they do still try to be very helpful and in being helpful do start to hide features and impose limitations. From hidden query caches which can't be manually cleared and eventually may eat too much memory, to an insistance that result set columns must have the same name as object properties to which they are mapped, micro-ORMs can still get in the way if you aren't careful. They are often a far superior option to full-fledged ORMs, however.

My recommendations for systems written in OOP languages which need to store persistant data go as follows:

1. If possible, try to use a suitable document database. There are many reasons why this may not be possible for you, so don't hurt yourself trying to fight the unwinnable fight.
1. If you have some ability to write your own SQL, know that you need more than just simple CRUD operations, and are looking for some niceties along the way, use a suitable micro-ORM.
1. **If** you must use SQL and **If** your models are extremely simple and **If** you only need basic CRUD operations and **If** you don't want to use CQRS, then you can use an ORM like EF or NHibernate.
1. If you want full control over your SQL and want a tool that knows when and how to just get out of your way, consider **CastIron**.

## Overview

CastIron is a bare-minimum, bare-metal implementation of the QueryObject pattern for SQL. You create an implementation of either `ISqlQuery` or `ISqlCommand` with your SQL code and query reading logic and pass it to an instance of `SqlQueryRunner`. The runner will do all the dirty stuff like opening the `SqlConnection`, creating the `SqlCommand` and obtaining an `SqlDataReader`. CastIron gives you all the tools to work with things like In/InOut parameters or multiple result sets. CastIron also provides tools to automatically map result sets to objects and `IEnumerable`s of objects. You can use these features if you want them, and ignore them if you don't. 

## Examples

Examples TBD.

Please see the existing unit tests for examples of using CastIron.Sql.

## Status

CastIron.Sql is currently very early in development. 



