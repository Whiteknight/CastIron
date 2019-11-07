﻿using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping.Compilers;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class MapCompiler : IMapCompiler
    {
        private static readonly ICompiler _allTypes;
        private static readonly ICompiler _arrays;
        private static readonly ICompiler _objects;

        static MapCompiler()
        {
            ICompiler allTypes = new DeferredCompiler(() => _allTypes);
            ICompiler arrays = new DeferredCompiler(() => _arrays);
            ICompiler objects = new DeferredCompiler(() => _objects);

            ICompiler scalars = new ScalarCompiler();
            ICompiler customObjects = new CustomObjectCompiler(allTypes, scalars);

            ICompiler tupleContents = new FirstCompiler(
                new IfCompiler(s => s.TargetType == typeof(object), objects),
                new IfCompiler(s => s.TargetType.IsSupportedPrimitiveType(), scalars),
                new IfCompiler(s => s.TargetType.IsMappableCustomObjectType(), customObjects),
                new FailCompiler()
            );
            ICompiler tuples = new TupleCompiler(tupleContents);

            ICompiler dictionaryContents = allTypes;
            ICompiler concreteDictionaries = new ConcreteDictionaryCompiler(dictionaryContents);
            ICompiler abstractDictionaries = new AbstractDictionaryCompiler(dictionaryContents);

            _objects = new ObjectCompiler(concreteDictionaries, scalars, arrays);

            ICompiler arrayContents = new FirstCompiler(
                scalars,
                objects
            );
            ICompiler concreteCollections = new ConcreteCollectionCompiler(arrayContents, customObjects);
            ICompiler abstractCollections = new AbstractCollectionCompiler(arrayContents, customObjects);
            
            _arrays = new ArrayCompiler(customObjects, arrayContents);

            _allTypes = new FirstCompiler(
                new IfCompiler(s => s.TargetType == typeof(object), objects),
                new IfCompiler(s => s.TargetType.IsSupportedPrimitiveType(), scalars),
                new IfCompiler(s => s.TargetType.IsArray && s.TargetType.HasElementType, arrays),
                new IfCompiler(s => s.TargetType.IsUntypedEnumerableType(), arrays),
                new IfCompiler(s => s.TargetType.IsConcreteDictionaryType(), concreteDictionaries),
                new IfCompiler(s => s.TargetType.IsDictionaryInterfaceType(), abstractDictionaries),
                new IfCompiler(s => s.TargetType.IsGenericType && s.TargetType.IsInterface && (s.TargetType.Namespace ?? string.Empty).StartsWith("System.Collections"), abstractCollections),
                new IfCompiler(s => s.TargetType.IsConcreteCollectionType(), concreteCollections),
                new IfCompiler(s => s.TargetType.IsTupleType(), tuples),
                new IfCompiler(s => s.TargetType.IsMappableCustomObjectType(), customObjects),
                new FailCompiler()
            );
        }

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context, IDataReader reader)
        {
            Argument.NotNull(context, nameof(context));

            // For all other cases, fall back to normal recursive conversion routines.
            var state = context.GetState(null, context.Specific);
            var expressions = _allTypes.Compile(state);
            var statements = expressions.Expressions.Concat(new[] { expressions.FinalValue }).Where(e => e != null);

            var lambdaExpression = Expression.Lambda<Func<IDataRecord, T>>(
                Expression.Block(typeof(T), expressions.Variables, statements), context.RecordParam
            );
            DumpCodeToDebugConsole(lambdaExpression);
            return lambdaExpression.Compile();
        }

        // Helper method to get a reasonably complete code listing, to help with debugging
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