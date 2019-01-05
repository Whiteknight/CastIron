# Building Complex Objects

One of the design challenges of EntityFramework is that queries of almost any complexity are mapped into a single query with a single result set. Even modest LINQ queries can turn into huge result sets with dozens of columns and significant mapping logic. Errors are hard to find and performance is completely outside of the developer's control.

CastIron instead embraces and encourages the idea of using multiple result sets in a query. Each individual query can be small, simple and focused on doing one thing right. The difficulty in this approach arises when we need to combine multiple result sets together to form a complex hierarchy of objects.

## Example: Load a Person and Details 

Consider the case of loading out person records from a database along with contact information:

```csharp
public class ContactMethod
{
    public int Id { get; set;}
    public int PersonId { get; set;}
    public string Type { get; set;}   // One of "Phone", "Email", etc
    public string Value { get; set;}  // The phone number or email address
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set;}

    public List<ContactMethod> ContactMethods { get; set; }
}
```

In the database we have two tables:

```sql
CREATE TABLE dbo.Person (
    Id INT NOT NULL PRIMARY KEY,
    [Name] VARCHAR(64) NOT NULL
)

CREATE TABLE dbo.ContactMethod {
    Id INT NOT NULL PRIMARY KEY,
    PersonId INT NOT NULL,
    [Type] VARCHAR(8) NOT NULL,
    [Value] VARCHAR(64) NOT NULL,

    FOREIGN KEY (PersonId) REFERENCES Person(Id)
}
```

This schema suggests two queries which may be useful, one to load a single person by ID and another to load a group of persons by some sort of criteria. The first of these might look something like this:

```sql
SELECT Id, [Name] FROM Person WHERE Id = @id;
SELECT Id, [Type], [Value] FROM ContactMethod WHERE PersonId = @id;
```

The later might be a bit more involved depending on the query. We'll look at that in a bit. First, let's put this all together into a single query object which loads a single person by ID:

```csharp
public class LoadOnePersonQuery : ISqlQuery<Person>
{
    private readonly int _id;

    public LoadOnePersonQuery(int id)
    {
        _id = id;
    }

    public bool SetupCommand(IDataInteraction cmd)
    {
        cmd.ExecuteText(@"
            SELECT Id, [Name] FROM Person WHERE Id = @id;
            SELECT Id, [Type], [Value] FROM ContactMethod WHERE PersonId = @id;");
        cmd.AddParameterWithValue("@id", _id)
    }

    public Person GetResults(IDataResults results)
    {
        var person = results.AsEnumerable<Person>().SingleOrDefault();
        if (person == null)
            return null;    // Communicate that result is not found
        results.AdvanceToNextResultSet();
        person.ContactMethods = results.AsEnumerable<ContactMethod().ToList();
        return person;
    }
}
```

We can execute this query using our `ISqlRunner`:

```csharp
var people = runner.Query(new LoadOnePersonQuery(5));
```

## Example: Load People with ForEachInnerJoin

The above example is clean and simple, but now we want to look at the case where we load multiple people by some criteria. For the sake of simplicity, we will do a simple paging operation, which we may use to display a large list of people on a webpage, only a few at a time. First, let's look at the SQL query we will want to use:

```sql
DECLARE @persons TABLE (
    Id INT NOT NULL PRIMARY KEY,
    [Name] VARCHAR(64) NOT NULL
)
INSERT INTO @persons(Id, [Name])
    SELECT 
        Id, [Name] 
        FROM Person 
        ORDER BY Id ASC OFFSET @start ROWS FETCH NEXT @pageSize ROWS ONLY;

SELECT Id, [Name] FROM @persons;
SELECT 
    cm.Id, cm.[Type], cm.[Value] 
    FROM 
        @persons p
        INNER JOIN
        ContactMethod cm
            ON p.Id = cm.PersonId;
```

To combine these two result sets into a single enumerable of objects, we have a few options. We could do something like a `.Join()` on these two result sets:

```csharp
var persons = results.AsEnumerable<Person>().ToList();
results.AdvanceToNextResultSet();
var contacts = results.AsEnumerable<ContactMethod().ToList();
var pairs = persons
    .Join(contacts.GroupBy(c => c.PersonId), p => p.Id, cm => cm.Key, (p, cm) => new {
        Person = p,
        ContactMethods = cm
    });
foreach (var pair in pairs)
    pair.Person.ContactMethods = pair.ContactMethods.ToList();
return persons;
```

We could get it a little cleaner if we change the Person class a little bit:

```csharp
public class Person
{
    public Person()
    {
        ContactMethods = new List<ContactMethod>();
    }

    public int Id { get; set; }
    public string Name { get; set;}

    public List<ContactMethod> ContactMethods { get; }
}
```

Now we can use an extension method provided by CastIron to simplify the join operation:

```csharp
var persons = results.AsEnumerable<Person>().ToList();
results.AdvanceToNextResultSet();
var contacts = results.AsEnumerable<ContactMethod().ToList();
persons.ForEachInnerJoin(contacts, p => p.Id, cm => cm.PersonId, (p, cm) => p.ContactMethods.Add(cm));
return persons;
```

Let's put this all together into a query object:

```csharp
public class LoadPageOfPersonsQuery : ISqlQuery<IReadOnlyList<Person>>
{
    private readonly int _start;
    private readonly int _pageSize;

    public LoadPageOfPersonsQuery(int start, int pageSize = 100)
    {
        _start = start;
        _pageSize = pageSize;
    }

    public bool SetupCommand(IDataInteraction cmd)
    {
        cmd.ExecuteText(@"
            DECLARE @persons TABLE (
                Id INT NOT NULL PRIMARY KEY,
                [Name] VARCHAR(64) NOT NULL
            )
            INSERT INTO @persons(Id, [Name])
                SELECT 
                    Id, [Name] 
                    FROM Person 
                    ORDER BY Id ASC OFFSET @start ROWS FETCH NEXT @pageSize ROWS ONLY;

            SELECT Id, [Name] FROM @persons;
            SELECT 
                cm.Id, cm.[Type], cm.[Value] 
                FROM 
                    @persons p
                    INNER JOIN
                    ContactMethod cm
                        ON p.Id = cm.PersonId;");
        cmd.AddParameterWithValue("@start", _start);
        cmd.AddParameterWithValue("@pageSize", _pageSize);
    }

    public IReadOnlyList<Person> GetResults(IDataResults results)
    {
        var persons = results.AsEnumerable<Person>().ToList();
        var contacts = results.GetNextEnumerable<ContactMethod();
        persons.ForEachInnerJoin(contacts, p => p.Id, cm => cm.PersonId, (p, cm) => p.ContactMethods.Add(cm));
        return persons;
    }
}
```

We can execute this query pretty simply from anywhere that we have an `ISqlRunner` available:

```csharp
var people = runner.Query(new LoadPageOfPersonsQuery(0, 100));
```

## Example: MapOnto

CastIron provides another nice extension method which might be helpful in some cases. It's like the `ForEachInnerJoin` method, but gives you an option to select the object you want instead of using LINQ to match keys:

```csharp
var persons = results
    .AsEnumerable<Person>()
    .ToDictionary(p => p.Id);
results
    .GetNextEnumerable<ContactMethod()
    .MapOnto(cm => persons[cm.PersonId], (cm, p) => p.ContactMethods.Add(cm));
return persons.Values;
```

The choice of whether to use one of the CastIron extension methods or normal LINQ methods or any other mechanism for combining result sets is up to your personal tastes.

## Example: Get a Count Too

One thing that often comes up when Paging results on a UI is the need to get a count of all results, so we can know how many pages to show. We would like a display on the bottom of the page that says something like "Page 5 of 400". To do that, we need to know how many items are in the database. We could add this into our existing `LoadPageOfPersonsQuery` object by creating a new `PagedResults` type and adding a count query to the end of our existing query sql:

```csharp 
public class PagedResults<T>
{
    public int Count { get; set; }
    public IList<T> Results { get; set; }
}

public class LoadPageOfPersonsWithCountQuery : ISqlQuery<PagedResults<Person>>
{
    private readonly int _start;
    private readonly int _pageSize;

    public LoadPageOfPersonsWithCountQuery(int start, int pageSize = 100)
    {
        _start = start;
        _pageSize = pageSize;
    }

    public bool SetupCommand(IDataInteraction cmd)
    {
        cmd.ExecuteText(@"
            DECLARE @persons TABLE (
                Id INT NOT NULL PRIMARY KEY,
                [Name] VARCHAR(64) NOT NULL
            )
            INSERT INTO @persons(Id, [Name])
                SELECT 
                    Id, [Name] 
                    FROM Person 
                    ORDER BY Id ASC OFFSET @start ROWS FETCH NEXT @pageSize ROWS ONLY;

            SELECT Id, [Name] FROM @persons;
            SELECT 
                cm.Id, cm.[Type], cm.[Value] 
                FROM 
                    @persons p
                    INNER JOIN
                    ContactMethod cm
                        ON p.Id = cm.PersonId;
            SELECT COUNT(0) FROM Persons;");
        cmd.AddParameterWithValue("@start", _start);
        cmd.AddParameterWithValue("@pageSize", _pageSize);
    }

    public PagedResults<Person> GetResults(IDataResults results)
    {
        var persons = results.AsEnumerable<Person>().ToList();
        var contacts = results.GetNextEnumerable<ContactMethod();
        persons.ForEachInnerJoin(contacts, p => p.Id, cm => cm.PersonId, (p, cm) => p.ContactMethods.Add(cm));
        var count = results.GetNextEnumerable<int>().Single();
        return new PagedResults<Person> {
            Count = count,
            Results = persons
        };
    }
}
```

However, it might be better to create a separate query class to get this so we can reuse it, and execute the two queries together as a batch on a single connection:

```csharp
public class GetPersonsCountQuery : ISqlQuerySimple<int>
{
    public string GetSql() => "SELECT COUNT(0) FROM Persons;";

    public int GetResults(IDataResults results)
    {
        return results.AsEnumerable<int>().First();
    }
}
```

With our `ISqlRunner` we can batch these two queries together and execute them at once:

```csharp
var batch = runner.CreateBatch();
var countPromise = batch.Add(new GetPersonsCountQuery());
var peoplePromise = batch.Add(new LoadPageOfPersonsQuery(0, 100));
runner.Execute(batch);
var count = countPromise.GetValue();
var people = peoplePromise.GetValue();
```

Performance in this case is nearly identical to the case of doing both things in a single query, but this approach allows each class to adhere to a single responsibility and allows these queries to be more easily reused. For example, if your UI is converted to use infinite scroll instead of manual paging, you probably don't need to get the count at all, and can just keep scrolling until the query returns no results. Likewise, there are many situations where we may want to learn the count of the People table without having to load any people from it.