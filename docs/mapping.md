# Mapping

Mapping results from an `System.Data.IDataReader` to an enumerable of values or objects is one of the core features of CastIron. However, this conversion is rarely a straight-forward task. CastIron provides a set of built-in mappers and mapper-builders to handle some of the complexities in a heuristic-based way. As with all other features of CastIron, the mappers are pluggable, so you can substitute your own implementation if you prefer.

All result mapping starts with an `IDataResults` or `IDataResultsStream`, which encapsulates the `System.Data.IDataReader` and some additional details. An `IDataResults` instance is automatically passed to the `Read()` method of your `IResultMaterializer<T>` (inherited by things like `ISqlQuery<T>` and certain `ISqlCommand<T>` variants). `IDataResultsStream` instances come from the `ISqlRunner.QueryStream()` method. In either case, the basics of mapping are the same.

If you would like to build up complex objects by mapping together multiple result sets, see the page on [Mapping Complex Objects](maponto.md) which shows several detailed examples.

## Output Parameters

`IDataResults` and `IDataResultsStream` provide access to output parameters from the query. However, due to the nature of the underlying `System.Data.IDataReader`, parameter values cannot be accessed until the reader (if any) is closed. If the command does not have a reader, output parameters can be accessed at any time.

This means that if your query has a reader, you should read all result sets first before attempting to access output parameters. When you access your output parameters, the reader will be closed and additional mapping will not work and may throw an exception.

## Old-Fashioned Manual Mapping

The most "traditional" and backwards-compatible way to read results is to get the raw `System.Data.IDataReader`:

```csharp
var reader = results.AsRawReader();
```

Once you call `AsRawReader()` the reader is consumed and the `IDataResults` object cannot be used anymore. This is an excellent stepping stone for a migration from old `System.Data.Sql` primitives to CastIron: Wrap your queries into an appropriate `ISqlQuery` variant, and use the raw reader to map results using your existing logic until you're reading to upgrade to the more automated mappings.

Notice that if you are using a normal query, the reader lifecycle will be managed for you and you do not need to `.Dispose()` the reader yourself. However, if you are streaming and you call `.AsRawReader()` you will assume control over the reader and will need to call `.Dispose()` yourself.

```csharp
// In a query object
public object Read(IDataResults results) {
    var reader = results.AsRawReader();
    ...
    // Call reader.Dispose() here
}
```

```csharp
// to stream data
using (var stream = runner.QueryStream(myQuery)) {
    using (var reader = stream.AsRawReader()) {
        ... 
    }
}
```

## Primitive Types

CastIron defines a list of "primitive types" as the following, including the `Nullable<T>` variants of each:

* `bool`
* `byte`
* `byte[]`
* `DateTime`
* `decimal`
* `double`
* `float`
* `Guid`
* `int`
* `long`
* `short`
* `string`
* `uint`
* `ulong`
* `ushort`

CastIron can typically convert most of the raw column values to any of the above primitive types so long as the data formats are compatible.

## `AsEnumerable<T>()`

The method `IDataResults.AsEnumerable<T>()` is the most basic mechanism to map a results stream from the database into an enumerable of objects. This method has several options which can be leveraged for different use-cases, and allows plugging in custom implementations of various algorithms. We'll discuss some of these options below.

## Object Mappings

```csharp
var enumerable = results.AsEnumerable<object>();
```

Mapping to an `object` works a little differently depending on how and when the mapping occurs.

### Top-Level `object` mapping

At the top level, if we call `.AsEnumerable<object>()` each row will be converted into an `IDictionary<string, object>` by column name:

```csharp
var firstRow = result.AsEnumerable<object>().First();
var asDict = firstRow as IDictionary<string, object>;
var intValue = asDict["IntValue"];
```

Notice that SQL allows multiple columns to have the same name. CastIron maps values according to this heuristic:

1. If there is only one column with the given name, the value will be mapped as a scalar using the type from the database, with `DBNull` converted to `null`.
2. If there is more than one column with the given name, the values will be mapped to an `object[]` array, with `DBNull` mapped to `null`.

Here's a quick example:

```sql
SELECT 1 AS Id, 'A' AS [Name], 'B' AS [Name];
```

```csharp
object obj = result.AsEnumerable<object>();
var dict = obj as Dictionary<string, object>;
var id = dict["Id"];    // 1
var names = dict["Name"] as object[];
var name1 = names[0].ToString(); // 'A'
var name2 = names[1].ToString(); // 'B'
```

CastIron does this because column name and ordering information is part of the metadata of the value and CastIron does not want to just throw data away if it doesn't know for sure the information will not be needed. In general, for all but the most fast-and-dirty queries, you'll want more control over the data structure than this and you should use a proper custom class instead of `object`.

### Named `object` mapping

If the `object` is nested somewhere that has a name (in a `Dictionary<string, object>` or as a property in a class, for example) CastIron will instead map it according to a different set of heuristics:

1. If there are no matching columns, the value is set to `null`
1. If there is exactly one matching column, the value is mapped as a scalar
1. If there is more than one matching column, the value is mapped to an `object[]` array

Let's show a few examples. First, we define our result type:

```csharp
public class ResultRow
{
    public int Id { get; set;}
    public object Name { get; set;}
}
```

Now let's look at some queries:

```sql
SELECT 1 AS Id;
```

In this first case, the `.Name` property will be `null`, there are no columns called `Name`.

```sql
SELECT 1 AS Id, 'A' AS [Name];
```

In this second case, the `.Name` property will be the string `"A"`.

```sql
SELECT 1 AS Id, 'A' AS [Name], 'B' AS [Name];
```

In this third case, the `.Name` property will contain an `object[]` array with the values `{ "A", "B" }`.

### Scalar `object` mapping

In cases where the object cannot be treated as a collection or a dictionary, the object will be treated as a scalar. In these cases, the value from exactly one column will be copied directly to the output (with `DBNull` converted to `null`).

## Object Array and Collection Mappings

If we ask CastIron to map rows to `object[]` arrays, we will get an array with values from each column in the native data type, with `DBNull` converted to `null`.

```csharp
object[] firstRow = result.AsEnumerable<object[]>().First();
object firstValue = firstRow[0];
```

`object[]` arrays can also be instantiated if we ask CastIron to map rows to any of the following types:

* `object[]`
* `IEnumerable<object>`
* `ICollection<object>`
* `IList<object>`
* `IReadOnlyList<object>`
* `IEnumerable`
* `IList`
* `ICollection`

Again, for all but the most quick-and-dirty queries, you'll probably want to use a real custom class instead of using a mapping like this.

## Tuple Mappings

If we ask CastIron to map to a `Tuple<>` type, each row will be converted to a tuple with the first column value going into the first tuple value, the second column into the second tuple value, etc. The maximum supported tuple size is 7, above which CastIron is (currently) unable to convert.

```csharp
var firstRow = result.AsEnumerable<Tuple<int, string, float>>().First();
int firstValue = firstRow.Item1;
string secondValue = firstRow.Item2;
float thirdValue = firstRow.Item3;
```

Each element of the tuple must be one of the Primitive Types listed above, `object`, or a custom object type. If `object`, it will be converted as a scalar with the value of a single column converted to the tuple parameter. `DBNull` will be converted to `null`.

Notice that if a custom object type is used as one of the tuple parameters, that object might greedily consume many rows and you might get fewer results than you expect. Here's an example:

```csharp
public class IdAndNames 
{
    public int Id { get; set; }
    public string[] Names { get; set; }
}

var results = result.AsEnumerable<Tuple<IdAndNames, IdAndNames>>("SELECT 1 AS Id, 'TestA' AS Names, 2 AS Id, `TestB` AS Names").Single();
results[0].Item1.Id == 1
results[0].Item2.Names == new string[] { "TestA", "TestB" };

results[0].Item1.Id == 2
results[0].Item2.Names == new string[0];
```

This is because tuples mapping gets columns in order without names, while objects greedily map columns by property<->column name (discussed more below). The first instance of `IdAndName` maps a single `Id` column and all the columns named `Name`, and then the second `IdAndName` instance is able to map the second `Id` column but there are no remaining unmapped columns for the `Names` property.

## ValueTuple Mappings

CastIron can map a ValueTuple struct in almost exactly the same way as it maps Tuple types. This can lead to some very succinct code. Compare to the first example from the Tuple mapping section above:

```csharp
var (firstValue, secondValue, thirdValue) = result.AsEnumerable<(int first, string second, float third)>>().First();
```

ValueTuple mappings are extremely useful when you want to map results of a query to domain objects, without having to explicitly create data model classes as an intermediate step.

## Primitive Mappings

If we ask CastIron to map a row to one of the primitive types, it will take the value from the first column of the result set and map it. Other columns will be ignored.

```csharp
IEnumerable<int> values = result.AsEnumerable<int>();
```

This is useful when we only want to select a single item from a DB, or a sequence of items in a single column. Additional columns in the result set, if any, will be ignored.

## Array and Collection Mappings

We can ask CastIron to map a row into an array or collection type of any of the primitive types listed above and their `Nullable<T>` variants, including mapping to other collection types:

```csharp
var arrayOfBools = result.AsEnumerable<bool[]>();
var listOfInts = result.AsEnumerable<List<int>>();
var iListOfNullableFloats = result.AsEnumerable<List<float?>>();
var iEnumerableOfString = result.AsEnumerable<IEnumerable<string>>();
```

By default, CastIron can support arrays of primitive types, any interface type which is assignable from `List<T>`, and any concrete type which implements `ICollection<T>` and has a default parameterless constructor (Where `T` is any of the primitive types) and `ISet<T>`. Some examples:

* `T[]`
* `IList<T>`
* `IReadOnlyList<T>`
* `ICollection<T>`
* `IEnumerable<T>`
* `List<T>`
* `HashSet<T>`
* `ISet<T>`
* Custom types which inherit from `ICollection<T>` and have a default parameterless constructor

In these situations, CastIron will map values from all columns to the specified primitive type and store all values from a single row into a single collection instance.

## Dictionary Mappings

CastIron can map a row to a dictionary where the key is a `string` name of the column and the value is the value of that column. The key will always be the name of the column, and the value will always be the value from that column or columns of the same name. CastIron can support:

1. `Dictionary<string, T>`
1. Any custom type which implements `IDictionary<string, T>` and has a default parameterless constructor
1. `IDictionary<string, T>`
1. `IReadOnlyDictionary<string, T>` 

(Where `T` is any of the primitive types or `object`). Some examples:

```csharp
var dicts = result.AsEnumerable<Dictionary<string, int>>();
var idicts = result.AsEnumerable<IDictionary<string, object>>();
var irodicts = result.AsEnumerable<IReadOnlyDictionary<string, float>>();
```

If the value type is `object` and there are multiple columns with the same name, the value will be instantiated as `object[]` and all values will be included. If the value is any other the other primitive types, only the first column with that name will be mapped.

### Example: ExpandObject

The `ExpandoObject` class implements `IDictionary<string, object>` so CastIron can map to it, and we can use it with the `dynamic` keyword:

```sql
SELECT 5 AS Id, 'CastIron' AS [Name], '0.7.0' AS [Version];
```

```csharp
dynamic expand = results.AsEnumerable<ExpandObject>().First();
int id = expand.Id;
string name = expand.Name;
string version = expand.Version;
```

## Custom Object Mapping

The above special cases can be interesting in some situations, but the most helpful and most common use of CastIron's mapping feature is to map the values of a row into the properties of a custom object type. However, there are some difficulties in this approach:

1. It's possible for a result set to have multiple columns with the same name, but it is not possible for an object to have multiple properties with the same name. If we want to map such a result set to an object without loss, we would need to map the same-named columns to some sort of collection type.
1. It's possible for an object to have properties which are themselves other objects, creating a hierarchical tree-like structure. Rows from the database are flat and do not support nesting. If we want to map values from a flat row into properties of nested objects, we need some kind of way to indicate how to map these.
1. It's possible for a column in a result set to not have any name. If we would like to map these values, we would need some way to inform the mapper which property should receive these values (with the same caveat about multiple columns needing to be mapped to a collection type).
1. It is possible for an object to have read-only properties with values which can only be set in the constructor. To map these values, the mapper must be able to pass some column values into the constructor parameters.

The default mapper in CastIron solves all of these problems using a series of heuristics designed to try and map the most data with the least ambiguity and loss of information.

See the page on [Mapping Complex Objects](maponto.md) for a more in-depth look at how to combine multiple result sets to build complex object graphs.

### Basic Property Mapping Heuristics

CastIron's mapper obeys the following heuristics and principles when mapping columns from a result set into the constructor parameters and writable public properties of an object:

1. Name matching of properties and parameters is always case-insensitive. When column names are used as keys in a `Dictionary<,>` the case of the original column name will be preserved.
1. If the property or parameter is a scalar type, the first column with a matching name will be mapped
1. If the property or parameter is a supported collection type, all columns with a matching name will be mapped
1. If the name of the property or parameter name matches, those columns will be mapped first
1. Otherwise, if the property is tagged with the `ColumnAttribute`, and the `ColumnAttribute.Name` property matches columns (case insensitive), those columns will be mapped
1. Otherwise, if the property or parameter is tagged with the `UnnamedColumnsAttribute`, any unnamed columns will be mapped, using the same rules about number of columns and scalars vs collections listed above (Notice that some DB providers don't support this option).
1. If the type of the property is `object`, the property will be mapped as a scalar if there is only one matching column, and will be mapped as `object[]` if there are multiple matching columns.
1. If the type of the property is a supported dictionary type or a custom child object type, the name of the property will be used as a prefix to match column names and the mapper will recurse using that name prefix, with a default separator of `"_"`.

It is possible to provide mappings which use other behaviors, but CastIron does not supply any of these (yet). `ColumnAttribute.Order` and `ColumnAttribute.TypeName` are not currently supported.

### Simple Example

First, consider the following class:

```csharp
public class Person
{
    public int Id { get; }
    public string Name { get; set;}
    public List<string> Nicknames { get; set; }

    public Person(int id)
    {
        Id = id;
    }
}
```

And consider this SQL statement which can pull the data from the database:

```sql
SELECT
    ID,
    FirstName + ' ' + LastName AS [Name],
    PrimaryNickname AS Nicknames,
    AlternateNickname AS Nicknames
    FROM
        People;
```

Finally, we perform the mapping by calling `AsEnumerable`:

```csharp
var people = results.AsEnumerable<Person>().ToList();
```

In this case, the `ID` column of each row will be mapped to the `id` constructor parameter and will be readonly. The `FirstName` and `LastName` columns will be combined and  mapped directly to the `Name` property. The `PrimaryNickname` and `AlternateNickname` columns will be mapped to the Nicknames collection. The mapping compiler will, after analyzing the result set, compile code very similar to the following:

```csharp
Person Map(IDataRecord r)
{
    Person instance = new Person((int)r.GetValue(0));
    instance.Name = (string)r.GetValue(1);
    List<string> list1 = new List<string>();
    list1.Add((string)r.GetValue(2));
    list1.Add((string)r.GetValue(3));
    instance.Nicknames = list1;
    return instance;
}
```

### Creating the Instance

With no options specified, CastIron will create an object instance by searching for a "best" constructor and mapping columns to constructor parameters. Once the object instance is created, public writeable properties will be assigned from matching columns. This is the default mechanism but is not the only supported one.

#### Best Match Constructor

The default behavior of CastIron is to search for the "best" matching constructor. The default heuristic for what constitutes "best" is to find the constructor with the largest number of parameters which can be mapped from columns in the result set. This heuristic can be overridden by providing your own custom `IConstructorFinder` instance:

```csharp
var objects = results.AsEnumerable<MyType>(c => c
    .For<MyType>(d => d.UseConstructorFinder(myConstructorFinder))
);
```

CastIron provides two types which can be used if you do not want to write your own:

* `CastIron.Sql.Mapping.BestMatchConstructorFinder` which uses the "best" heuristic above and
* `CastIron.Sql.Mapping.DefaultOnlyConstructorFinder` which only uses the default parameterless constructor, and forces all columns to map to public properties.

Instead of these two, you can provide your own custom instance if you want to have different behavior.

#### Factory Methods

You can use a custom factory method to provide an instance.

```csharp
var objects = results.AsEnumerable<MyType>(c => c
    .For<MyType>(d => d.UseFactory(r => new MyType()))
);
```

Also note that there's no way for CastIron to "consume" columns from the reader. If you use a value from the reader in the factory method, those columns will still be used to map properties later in the mapping algorithm. 

#### Preferred Constructors

You can manually specify a constructor to use, if you want to have that control. There are two overrides for method `.UseConstructor()` which allow you to specify which one to use. The first takes a `ConstructorInfo` parameter, and the second takes an array of types which are used to lookup the constructor:

```csharp
var objects = results.AsEnumerable<MyType>(c => c
    .For<MyType>(d => d.UseConstructor(constructorInfo))
);

var objects = results.AsEnumerable<MyType(c => c
    .For<MyType>(d => d.UseConstructor(new Type[] { ... }))
);
```

The constructor provided may not be `null`, may not be a `static`, `private` or `protected`. It must be the constructor for the class you are trying to map. Failure of any of these checks will cause an exception to be thrown.

## Map Compilers

CastIron doesn't just map a result set to an enumerable of values. First it compiles an efficient mapping function and then it invokes that mapping function on every row in the result set. This compilation step takes extra time at the beginning, but mapping individual rows is faster thereafter.

You can implement your own mapper compiler by implementing the `IMapCompiler` interface in your own custom class. You can use your custom map compiler from the `IDataResults.AsEnumerable<T>()` method:

```csharp
var enumerable = results.AsEnumerable<MyCustomType>(c => c
    .For<MyCustomType>(d => d.UseCompiler(myCompiler))
);
```

### Caching

CastIron does not automatically cache mappings, because in some use-cases would generate lots of one-off mappings, which could fill the cache and use up large amounts of memory. Instead, CastIron has an opt-in cache which you can use as needed:

```csharp
results.CacheMappings(true, key);
```

The `key` is the unique lookup key used to identify the map inside the cache. If not provided, the key will default to the query object instance being executed. This is useful if you have a Query object instance which you reuse over and over again, with maybe only some parameter values changing between each execution. If you aren't reusing the same Query object instance, you should probably use a custom key instead, such as a string name, or even the raw text of the SQL itself.

The cached mapping is highly dependent on both the object being mapped and the structure of the result set. The number, order and data types of the columns is critical. So long as the columns are the same, the cached map function will work as expected. If the columns change in any way, the map may throw all sorts or errors or have all sorts of weird effects.

The cache can be inspected, cleared or manipulated from the `ISqlRunner` instance:

```csharp
runner.MapCache.Clear();
```

#### Setting the Cache Instance

By default, every `ISqlRunner` instance is created with a new cache instance, and cached mappings are not shared between runners. However, this might not be the best use-case for your application. You can specify a custom cache when you create the runner:

```csharp
var runner = RunnerFactory.Create(connectionString, mapCache: myCache);
```

There is a default global instance of a cache which you can use, as one way to share caches between multiple runners:

```csharp
var globalCache = CastIron.Sql.Mapping.MapCache.GetDefaultInstance();
var runner = RunnerFactory.Create(connectionString, mapCache: globalCache);
```

### Setting Up Your Compiler

Map compilers are broken down into three objects:

1. The `IScalarMapCompiler` which handles mapping specific column types to specific value types. For example there is a scalar map compiler to map any column type to a `string` using the `.ToString()` method.
2. The `ICompiler` which handles mapping an aggregate type such as a tuple, array or custom object.
3. The `IMapCompiler` type which orchestrates the two and performs the mapping.

There is another object, the `IMapCompilerSource` which keeps track of `IScalarMapCompiler` and `ICompiler` instances and acts like a factory for `IMapCompiler`.

```csharp
var myMapCompilerSource = new CastIron.Sql.Mapping.MapCompilerSource();
var runner = RunnerFactory.Create(connectionString, compilerSource: myMapCompileSource);
```

You can provide a custom implementation of `IMapCompilerSource` if you are brave enough, or you can modify an existing one. 

### Using Subclasses

Sometimes you want to instantiate different subclasses depending on the contents of the row. For example, consider the following type hierarchy:

```csharp
public abstract class Pet {
    public string Name { get; set;}
}

public class Dog : Pet {
    public bool IsAGoodDog { get; set; }
}

public class Cat : Pet {
    public int LevelOfGeneralDisdain { get; set; }
}

public class Exotic : Pet { }
```

And this pseudo-SQL:

```sql
SELECT AnimalType, [Name], IsAGoodDog, LevelOfGeneralDisdain FROM Animals;
```

You can create different mappings for rows which represent Dogs from rows which represent Cats by reading the `AnimalType` column:

```csharp
var pets = results.AsEnumerable<Pet>(s => s
    .For<Pet>(p => p

        // Default type, if no other predicates match
        .UseClass<Exotic>()

        // Predicates tested in order until a match is found
        .UseSubclass<Dog>(r => r.GetString(0) == "dog")
        .UseSubclass<Cat>(r => r.GetString(0) == "cat")
    )
);
```

Predicates are evaluated in the specified order, so when there is overlap the first predicate which matches will select the subclass to use.

### Using a Custom Mapping

A Map is a delegate which takes an `IDataRecord` and returns your desired object type. The mapper compilers build these mappers automatically, but you can specify your own if you want the control, or have existing logic which you are trying to port to CastIron.

```csharp
var enumerable = results.AsEnumerable<MyResultType>(c => c
    .For<MyResultType>(d => d.UseMap(r => new MyResultType { ... }))
);
```

This option is only one level of abstraction higher than the `.AsRawReader()` method and is only really useful as a step to migrate existing data access code to use CastIron. Where possible, try to use one of the existing maps or map compilers to save yourself work and potential sources of bugs.
