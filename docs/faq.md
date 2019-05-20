# Frequently Asked Questions

**Should Queries and Commands be wrapped up into stored procedures for better performance?**

Stored procs can be an optimization, but not necesarily a huge one. What you save with a stored proc is generally the cost of repeatedly parsing the SQL syntax and compiling it into an execution plan. There are a few problems with stored procs:

1. Since they live in the DB, it can be very difficult to get them into version control and keep them synchronized with the application code.
1. Many organizations also demand extensive boiler-plate templates for stored procs, which can wipe out any performance gains (sometimes there are good reasons for this, often times it's just cargo-cult nonsense).
1. Cached execution plans may become suboptimal as data characteristics change and will require manual intervention to recompile

Stored Procs can be an optimization, but for most work they are a premature optimization.

CastIron easily supports calling stored procs from your query and command classes, so it should be an easy transition to prototype the raw SQL in a query and then move that to a stored proc call later as required. It's probably worthwhile to do some benchmarking to make sure that the change of query to stored proc actually does lead to performance benefits, and not just maintainability headaches.

**Can I use CastIron to wrap existing data accesses such as Dapper?**

Yes. CastIron provides a `ISqlConnectionAccessor` interface which gives access to the raw underlying `IDbConnection` object, which Dapper or other micro-ORMs can operate on. It is probably unnecessary to be using two micro-ORMs to manage your SQL connections, but it is possible if you want to do it.

**Why not just use EntityFramework?**

EntityFramework and other ORMs like NHibernate are big and bulky. They provide a lot of features, but often at a significant cost. Here are some costs to ORMs which must be considered for many projects:

1. The SQL queries generated are often problematic in terms of performance, readability and debuggability, and developers often have no real control over how SQL syntax is generated.
1. Features like automatic change detection and lazy loading can be great for prototyping but can cause real problems in production systems with concurrent accesses and tightly-controlled object lifecycles.
1. ORMs encourage and often require writing queries as LINQ instead of raw SQL, which provides access to only a limited subset of SQL functionality
1. Code-First schema generation for large and complex systems can often become harder to manage than just writing SQL schema statements, especially when features like custom indices and constraints are used, or when local naming conventions differ from what EF generates by default.
1. When things go wrong with EntityFramework, problems can often be extremely difficult to debug. Error messages are often obtuse, and Exceptions often lack sufficient information to track down a problem quickly.

ORMs like EntityFramework and NHibernate can be very good choices for many systems. However, for systems which are extremely simple ORMs can be overkill and for systems above a certain complexity threshold you can run into significant problems.

**Why not just use Dapper?**

Micro-ORMs like Dapper are very close in spirit to CastIron. The major differences (besides feature set and general level of code maturity) is in the structure and organization of code. Dapper represents a more simple, streamlined way to execute queries and get results, where CastIron is more opinionated about structure and design.

CastIron initially started life as an extension package for Dapper to implement the Query Object pattern, though this period was brief. It was decided that CastIron wanted more control over the lifecycle of the `SqlConnection` and wanted more interface points for pluggability than Dapper offered.

**CastIron doesn't map objects exactly the way I want**

Mapping between the relational database and the object-oriented data models is always going to be lossy and imperfect. CastIron uses several heuristics to guide the mapping algorithms, and focuses on trying to map information as well as possible without sacrificing performance. If CastIron's mappers don't map things exactly the way you want or expect, there are a few options which may be available to you:

1. Adjust the text of the SQL query to better fit the structure of the objects you want to populate
1. Adjust the structure of your objects to better fit the data which is being returned by the query
1. Use `IDataReader` directly to take full control over the mapping logic
1. Write your own `IMapCompiler` implementation (code contributions welcome!) to create better mappings for your use-case
1. Use the Object Mapper pattern to manually map a temporary Data Model to a better structure

**Can CastIron help with SQL query generation?**

It seems obvious that if we can map from columns in a result set to objects, that we should also be able to map from properties on an object to columns in a `SELECT` query or an `INSERT` statement. CastIron does offer a very modest amount of help here, but it's really quite a large problem and cannot be solved automatically (even with aggressive heuristics) like result set mapping can be. 

Consider a case of a single logical object being spread across two or more tables with matching primary keys. There's no obvious way for CastIron to know that we need to `INNER JOIN` these tables to get a complete object (and, if all of the involved tables have a column called `Id`, which one do we use as the object's `.Id` property?). Now consider a larger and more common case of mapping a heirarchy of objects into a large group of tables joined by foreign keys. CastIron can't know which foreign keys to follow and how to map all these objects in a general case without the user having to supply a significant amount of metadata. All this complicated metadata is exactly what separates an ORM like EntityFramework from a Micro-ORM like CastIron. 

Even if CastIron did manage enough metadata to map object hierarchies to tables and foreign keys, we'd only be able to generate `SELECT`, `INSERT` and `DELETE` statements. In order to automatically generate good `UPDATE` statements, we'd need to implement a change-tracking mechanism also. Again, this is a big complicated chunk of code and it's what separates the heavier ORMs from the lighter Micro-ORMs. 

The CastIron philosophy is that SQL is a powerful and expressive tool, and using an abstraction like an ORM to hide the power and expressivity of SQL from the programmer is not best practice. CastIron doesn't want to generate SQL, because CastIron will never be able to generate it as well as a skilled human practitioner can. If you want to be able to query the database from pure C# code without writing any SQL, other technologies will probably be better than CastIron.

**Can CastIron help with creating Tables?**

For the same reason as the above question about generating queries, the answer is "no". CastIron doesn't claim to know how to generate good SQL, it's expected that the developers of your project are proficient enough in SQL to do that themselves. 