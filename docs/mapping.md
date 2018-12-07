# Mapping

All result mapping starts with an `IDataResults` or `IDataResultsStream`. An `IDataResults` instance is automatically passed to the `Read()` method of your `ISqlQuery` and certain `ISqlCommand<T>` variants. `IDataResultsStream` instances come from the `ISqlRunner.QueryStream()` method. In either case, the basics of mapping are the same.

## The Old-Fashioned Way

The most "traditional" and backwards-compatible way to get results is to get the raw `IDataReader`:

```csharp
var reader = results.AsRawReader();
```

Once you call `AsRawReader()` the reader is consumed and the `IDataResults` object cannot be used anymore. This is an excellent stepping stone for a migration from old `System.Data.Sql` primitives to CastIron: Wrap your queries into an appropriate `ISqlQuery` variant, and use the raw reader to map results until you're reading to upgrade to the more automated mappings.

## `AsEnumerable()`

The method `IDataResults.AsEnumerable<T>()` is the most basic mechanism to map a results stream from the database into an enumerable of objects. This method has several overloads and options which can be leveraged for different use-cases: It can take a mapper delegate to convert each row to objects, It can take a mapper compiler to build a mapper which converts each row. It can also take a preferred constructor and a factory method in some cases.

### The Default Mapper Compiler

When you call `AsEnumerable<T>()` without any arguments, the default mapper is used

```csharp
var enumerable = results.AsEnumerable<MyResultType>();
```

The default mapper is a combined mapper which uses most of the default mapping logics of CastIron. You can access this mapper instance directly:

```csharp
var defaultMapper = CastIron.Sql.Mapping.CachingMappingCompiler.GetDefaultInstance();
```

The default mapper uses caching to keep the compiled mappers around for better performance. The caching compiler wraps the `RecordMapperCompiler()`, which dispatches to appropriate mappers by type. See the "Mapper Compilers" section below for more details on each individual mapper. Some of the types that the mapper supports by default are:

* `object` and `object[]` both map each result row to an array of `object` using the `IDataRecord.GetResults()` method. This object array will contain `DBNull` instead of `null` where values are missing.
* `string[]` mapps each result row to an array of string by calling `.ToString()` on each entry, except `DBNull` which is converted to `null`.
* `Tuple<...>` types map each result row to a tuple of the given types, by ordinal index. The first column of the reader is converted to the first parameter of the Tuple constructor, etc.
* For any other requested type, the mapper compiler builds a mapper delegate by assigning named columns to constructor parameters and public writable properties of matching names (case-insensitive).

### Using a Custom Mapper Compiler

One overload of `AsEnumerable<T>()` allows for the use of a custom mapper:

```csharp
IEnumerable<T> AsEnumerable<T>(IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null);
```

The first argument is the compiler to use use, which defaults to the default compiler if `null`. The remaining parameters allow you to specify additional options to the compiler. Notice that not all compilers respect these parameters.

* `factory` allows you to specify a factory method to create the record. Specifying a factory method disables caching for the generated mapper if used with the caching mapper compiler.
* `preferredConstructor` allows you to specify a particular constructor to use. By default, the property-and-constructor compiler will select a "best" constructor according to a matching heuristic. Specifying the preferred constructor allows you to pick which constructor to use and avoid the matching algorithm

See the section below on "Mapper Compilers" which lists the built-in mapper compilers.

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
    .UseSubclass<Dog>(r => r.GetString(0) == "dog")
    .UseSubclass<Cat>(r => r.GetString(0) == "cat")
    .Otherwise<Exotic>());
```

Predicates are evaluated in the specified order, so when there is overlap the first predicate which matches will select the subclass to use. The `Otherwise()` method specifies which subclass to use when all the other predicates fail. The methods `UseSubclass()` and `Otherwise()` both allow to specify normal mapping options like the mapper compiler to use.

### Using a Custom Mapping

A Mapper is a delegate which takes an `IDataRecord` and returns your desired object type. The mapper compilers build these mappers automatically, but you can specify your own if you want the control.

```csharp
var enumerable = results.AsEnumerable<MyResultType>(r => new MyResultType { ... });
```

This overload is one level of abstraction higher than the `AsRawReader()` method and typically requires more work to implement than the mapper compilers.

## Mapper Compilers

There are several `IRecordMapperCompiler` types in the `CastIron.Sql.Mapping` namespace which you can use. The default mapper will automatically select the most appropriate one based on the type and options you provide, but if you would like more control or to provide custom behavior you can specify one of these.

### `CachingMappingCompiler`

The `CachingMappingCompiler` is a Decorator around another compiler which provides caching behaviors. If the mapper can be cached, it will be held in memory. This provides a nice performance boost when the same queries are executed over and over again.

```csharp
var compiler = new CachingMappingCompiler(innerCompiler);
```

To clear the cache, which may be necessary in some situations, you can call the `ClearCache()` method:

```csharp
compiler.ClearCache();
```

The default compiler is an instance of `CachingMappingCompiler`. You can get a reference to it if, for example, you want to clear the default cache:

```csharp
var defaultCompiler = CachingMappingCompiler.GetDefaultInstance();
```

### `ObjectRecordMapperCompiler`

The `ObjectRecordMapperCompiler` maps the `IDataReader` into an enumerable of `object` or `object[]`. If there is exactly one column and the requested type is `object`, the value returned will be the object from the column. Otherwise, the returned value will be `object[]` cast to the requested type. This object array will include `DBNull` instances instead of `null` which will need to be detected and converted in your application if desired.

The supported types of this compiler are:

* `object`
* `object[]`
* `IEnumerable<object>`
* `ICollection<object>`
* `IList<object>`
* `IReadOnlyList<object>`
* `IEnumerable`
* `IList`
* `ICollection`

### `StringRecordMapperCompiler`

The `StringRecordMapperCompiler` maps the `IDataReader` into an enumerable of `string[]`. This compiler calls `.ToString()` on each entry in the record, except for `DBNull` which is converted to `null`. The following types are supported by this compiler:

* `string[]`
* `IEnumerable<string>`
* `IList<string>`
* `IReadOnlyList<string>`

### `PrimitiveRecordMapperCompiler`

The `PrimitiveRecordMapperCompiler` is used to map a value from a single column of the record to the given primitive type via unboxing and converstion. If the value is `DBNull` the default value for that type is returned instead. This mapper supports the following types and their `Nullable<T>` variants where available:

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

### `TupleRecordMapperCompiler`

The `TupleRecordMapperCompiler` maps a row into a `Tuple<>` by ordinal column index. The first column becomes the first parameter, the second column the second parameter, etc. This compiler can handle a Tuple with up to 7 values. This compiler expects each type to be a primitive value (see the list under "`PrimitiveRecordMapperCompiler`" above) and cannot map where the tuple type parameter is a class.

### `PropertyAndConstructorRecordMapperCompiler`

The `PropertyAndConstructorRecordMapperCompiler` is the most advanced compiler in CastIron. It maps a row into an object by matching constructor parameters and public writable properties by name. Name matching is case insensitive. This compiler first creates an instance by using either a supplied factory method or else it will find an appropriate constructor. When the object is instantiated, any remaining columns from the result set will be mapped to public properties on the instance.

#### Creating the Instance

If a `factory` method is provided, the compiler will use the factory method to create the instance and will not attempt to match constructor parameters.

If a `preferredConstructor` is provided, the compiler will use the given constructor and will match constructor parameters by name. Constructor parameters which do not have a matching column or a column which cannot be mapped, will be given a default value.

If `factory` and `preferredConstructor` are both omitted, the compiler will find the best "matching" constructor. The best match is the constructor with the largest number of parameters which correspond to column names in the result set. Constructors with parameters that do not correspond to a column will be ignored. Constructors with parameters which are not mappable primitive types (see the list under "`PrimitiveRecordMapperCompiler`") will be ignored. If a suitable constructor cannot be found, an exception will be thrown.

Constructor parameters which can be mapped will be one of the primitive types (see "`PrimitiveRecordMapperCompiler`" above) or a supported collection type (see "Collection Types" below).

#### Mapping Properties

Public properties with public `set` methods can be mapped to columns with the same name (case insensitive). The properties must either be one of the primitive types (see "`PrimitiveRecordMapperCompiler`" above) or a supported collection type (see "Collection Types" below).

#### Collection Types

Constructor parameters and public properties can be collection types of the supported primitive types (see "`PrimitiveRecordMapperCompiler`" above for the complete list). All columns the in `IDataRecord` with a matching name will be added to the collection with that name.

Array types will be instantiated directly and filled by assigning to array indices.

Interface types which inherit from `ICollection<T>` will be instantiated as a `List<T>` and elements added with the `.Add()` method.

Concrete types which inherit from `ICollection<T>` will be instantiated by invoking the default parameterless constructor. Elements will be added by calling the `.Add()` method. Any custom collection type which implements `ICollection<T>`, has a default parameterless constructor and implements the `.Add()` method can be used for this purpose. 

For example, the following properties can all be mapped:

```csharp
public string[] MyStrings { get; set;}

public IList<int> MyInts { get; set;}   // instantiated as List<int>

public HashSet<double> MyDoubles { get; set;}
```

### `RecordMapperCompiler` 

The `RecordMapperCompiler` dispatches to other compilers depending on the type of object requested. For example, if an `object[]` is requested, it will dispatch to a `ObjectRecordMapperCompiler` or if a custom object is requested it will dispatch to `PropertyAndConstructorRecordMapperCompiler`. 
