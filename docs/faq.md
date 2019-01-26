# Frequently Asked Questions

**Should Queries and Commands be wrapped up into stored procedures for better performance?**

Stored procs can be an optimization, but not necesarily a huge one. What you save with a stored proc is generally the cost of repeatedly parsing the SQL syntax and compiling it into an execution plan. There are a few problems with stored procs:

1. Since they live in the DB, it can be very difficult to get them into version control and keep them synchronized with the application code.
1. Many organizations also demand extensive boiler-plate templates for stored procs, which can wipe out any performance gains (sometimes there are good reasons for this, often times it's just cargo-cult mentality nonsense).
1. Cached execution plans may become suboptimal as data characteristics change and will require manual intervention to recompile

Stored Procs can be an optimization, but for most work they are a premature optimization.

CastIron easily supports calling stored procs from your query and command classes, so it should be an easy transition to prototype the raw SQL in a query and then move that to a stored proc call later as required. It's probably worthwhile to do some benchmarking to make sure that the change of query to stored proc actually does lead to performance benefits, and not just maintainability headaches.

**Can I use CastIron to wrap existing data accesses such as Dapper?**

Yes. CastIron provides a `ISqlConnectionAccessor` interface which gives access to the raw underlying `IDbConnection` object, which Dapper or other micro-ORMs can operate on.

**Why not just use EntityFramework?**

EntityFramework and other ORMs like NHibernate are big and bulky. They provide a lot of features, but often at a significant cost. Here are some costs to ORMs which must be considered for many projects:

1. The SQL queries generated are often problematic in terms of performance, readability and debuggability, and developers often have no real control over how SQL syntax is generated.
1. Features like automatic change detection and lazy loading can be great for prototyping but can cause real problems in production systems with concurrent accesses and tightly-controlled object lifecycles.
1. ORMs encourage and often require writing queries as LINQ instead of raw SQL, which provides access to only a limited subset of SQL functionality
1. Code-First schema generation for large and complex systems can often become harder to manage than just writing SQL schema statements, especially when features like custom indices and constraints are used, or when local naming conventions differ from what EF generates by default.
1. When things go wrong with EntityFramework, problems can often be extremely difficult to debug. Error messages are often obtuse, and Exceptions often lack sufficient information to track down a problem quickly.

ORMs like EntityFramework and NHibernate can be very good choices for many systems, but when your complexity goes beyond a certain threshold or when your use-cases don't perfectly fit their model, you can run into significant problems.

**Why not just use Dapper?**

Micro-ORMs like Dapper are very close in spirit to CastIron. The major differences (besides feature set and general level of code maturity) is in the structure and organization of code. Dapper represents a more simple, streamlined way to execute queries and get results, where CastIron is more opinionated about structure and design.

CastIron initially started life as an extension package for Dapper to implement the Query Object pattern, though this period was brief. It was decided that CastIron wanted more control over the lifecycle of the `SqlConnection` and wanted more interface points for pluggability than Dapper offered.