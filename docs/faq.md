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