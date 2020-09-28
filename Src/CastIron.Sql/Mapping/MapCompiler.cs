using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping.Compilers;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Basic IMapCompiler implementation, creates the ICompiler graph and constructs a map lambda from the
    /// resulting compiled expression list
    /// </summary>
    public class MapCompiler : IMapCompiler
    {
        private readonly ICompiler _compiler;

        public MapCompiler(IEnumerable<IScalarMapCompiler> scalarMapCompilers)
        {
            Argument.NotNull(scalarMapCompilers, nameof(scalarMapCompilers));
            _compiler = CreateCompiler(scalarMapCompilers);
        }

        private static ICompiler CreateCompiler(IEnumerable<IScalarMapCompiler> scalarMapCompilers)
        {
            // Setup some deferrals to avoid circular references
            ICompiler _allTypes = null;
            ICompiler allTypes = new DeferredCompiler(() => _allTypes);
            ICompiler _arrays = null;
            ICompiler arrays = new DeferredCompiler(() => _arrays);
            ICompiler _objects = null;
            ICompiler objects = new DeferredCompiler(() => _objects);
            ICompiler maybeObjects = new IfCompiler(s => s.TargetType == typeof(object), objects);

            // Scalars are primitive types which can be directly converted to from column values
            ICompiler scalars = new ScalarCompiler(scalarMapCompilers);
            ICompiler maybeScalars = new IfCompiler(s => s.TargetType.IsSupportedPrimitiveType(), scalars);

            // Custom objects are objects which are not dictionaries, collections, tuples or object
            // Needs to handle scalars separately because of the set-in-place requirements
            ICompiler customObjects = new CustomObjectCompiler(allTypes, scalars);
            ICompiler maybeCustomObjects = new IfCompiler(s => s.TargetType.IsMappableCustomObjectType(), customObjects);

            // Tuple compiler fills by index, so tuple values cannot be things which consume all contents
            // greedily (collections, dictionaries)
            ICompiler tupleContents = new FirstCompiler(
                maybeObjects,
                maybeScalars,
                maybeCustomObjects
            );
            ICompiler tuples = new TupleCompiler(tupleContents);
            ICompiler maybeTuples = new IfCompiler(s => s.TargetType.IsTupleType(), tuples);

            // Dictionaries consume contents greedily and fill in as much as possible by column name
            ICompiler dictionaryContents = allTypes;
            ICompiler concreteDictionaries = new ConcreteDictionaryCompiler(dictionaryContents);
            ICompiler maybeConcreteDictionaries = new IfCompiler(s => s.TargetType.IsConcreteDictionaryType(), concreteDictionaries);
            ICompiler abstractDictionaries = new AbstractDictionaryCompiler(dictionaryContents);
            ICompiler maybeAbstractDictionaries = new IfCompiler(s => s.TargetType.IsDictionaryInterfaceType(), abstractDictionaries);

            // "object" compiler may instantiate a dictionary, scalar or array depending on location in the
            // object graph and number of available columns
            _objects = new ObjectCompiler(concreteDictionaries, scalars, arrays);

            // arrays and collections can contain scalars which don't consume greedily
            ICompiler nonScalarCollectionContents = new FirstCompiler(
                maybeTuples,
                maybeConcreteDictionaries,
                maybeAbstractDictionaries,
                maybeCustomObjects
            );
            ICompiler concreteCollections = new ConcreteCollectionCompiler(scalars, nonScalarCollectionContents);
            ICompiler abstractCollections = new AbstractCollectionCompiler(scalars, nonScalarCollectionContents);

            // We instantiate arrays for array types and collections without a specified element type
            // (IEnumerable, ICollection, IList)
            _arrays = new ArrayCompiler(customObjects, scalars);
            ICompiler maybeArrays = new FirstCompiler(
                new IfCompiler(s => s.TargetType.IsArrayType(), arrays),
                new IfCompiler(s => s.TargetType.IsUntypedEnumerableType(), arrays)
            );

            // All the possible mapping types, each of which behind a predicate
            _allTypes = new FirstCompiler(
                maybeObjects,
                maybeScalars,
                maybeArrays,
                maybeConcreteDictionaries,
                maybeAbstractDictionaries,
                new IfCompiler(s => s.TargetType.IsAbstractCollectionType(), abstractCollections),
                new IfCompiler(s => s.TargetType.IsConcreteCollectionType(), concreteCollections),
                maybeTuples,
                maybeCustomObjects
            );
            return _allTypes;
        }

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> Compile<T>(MapContext operation, IDataReader reader)
        {
            Argument.NotNull(operation, nameof(operation));

            // For all other cases, fall back to normal recursive conversion routines.
            var state = operation.CreateContextFromDataReader(reader);
            var expressions = _compiler.Compile(state);
            var statements = expressions.Expressions.Concat(new[] { expressions.FinalValue }).Where(e => e != null);

            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(
                Expression.Block(
                    typeof(T),
                    expressions.Variables,
                    statements
                ),
                operation.RecordParam
            );
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        // Helper method to get a reasonably complete code listing, to help with debugging
        // If you want to see the generated code from the compiler, set a breakpoint here
        [Conditional("DEBUG")]
        private static void DumpCodeToDebugConsole(Expression expr)
        {
            // This property is marked private, so we can only get to it by reflection, which would be terrible if
            // this wasn't a debug-only helper routine
            var debugViewProp = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (debugViewProp == null)
                return;
            var code = (string)debugViewProp.GetGetMethod(true).Invoke(expr, null);
            Debug.WriteLine(code);
        }
    }
}