using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Attempt to instantiate a collection to fill a slot of type IList, IReadOnlyList, 
    /// ICollection, IEnumerable, etc
    /// </summary>
    public class AbstractCollectionCompiler : ICompiler
    {
        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        public AbstractCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        private struct ConcreteCollectionInfo
        {
            public ConcreteCollectionInfo(Type concreteType, Type elementType, ConstructorInfo constructor, MethodInfo addMethod)
            {
                ConcreteType = concreteType;
                ElementType = elementType;
                AddMethod = addMethod;
                Constructor = constructor;
            }

            public Type ConcreteType { get; }
            public Type ElementType { get; }
            public MethodInfo AddMethod { get; }
            public ConstructorInfo Constructor { get; }
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            // Get the concrete type to construct, the element type to populate, and the Add
            // method to call for each value
            var info = GetConcreteTypeInfo(context);
            var collectionType = info.ConcreteType;
            Debug.Assert(collectionType != null);
            Debug.Assert(context.TargetType.IsAssignableFrom(collectionType));

            var elementType = info.ElementType;
            Debug.Assert(elementType != null);

            var addMethod = info.AddMethod;
            Debug.Assert(addMethod != null);

            var constructor = info.Constructor;
            Debug.Assert(constructor != null);

            var originalTargetType = context.TargetType;
            context = context.ChangeTargetType(collectionType);

            // Get a list of expressions to create the collection instance and populate it with
            // values
            var collectionVar = GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = GetCollectionPopulateStatements(context, elementType, collectionVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                collectionVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(collectionVar.FinalValue, originalTargetType),
                collectionVar.Variables.Concat(addStmts.Variables)
            );
        }

        private ConcreteCollectionInfo GetConcreteTypeInfo(MapTypeContext context)
        {
            // By the time we get into this Compiler we have already limited the valid TargetType
            // values which are possible. At this point we either have a non-generic collection 
            // interface (IEnumerable, ICollection, IList) or we have a generic collection
            // interface type (IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IList<T>
            // or IReadOnlyList<T>). Separate out because we have different strategies for each
            if (context.TargetType.IsGenericType && context.TargetType.GenericTypeArguments.Length > 0)
                return GetConcreteTypeInfoForGenericInterfaceType(context);

            return GetConcreteTypeInfoForInterfaceType(context);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfoForGenericInterfaceType(MapTypeContext context)
        {
            // We have one of the interfaces IEnumerable<T>, ICollection<T>, 
            // IReadOnlyCollection<T>, IList<T> or IReadOnlyList<T>, get T from generic type
            // definition
            var elementType = context.TargetType.GenericTypeArguments[0];

            Type collectionType = null;

            // See if we have a custom type configured which we should use instead of List<T>
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != null && typeSettings.BaseType != typeof(object))
                collectionType = typeSettings.GetDefault().Type;

            // Just use List<T> if there is no custom type
            if (collectionType == null)
                return GetListOfTTypeInfo(elementType);

            // The type should have an Add(T) method (even if the interface we're using is 
            // IReadOnly...<T>). The Add method might not be exposed through the interface.
            var addMethod = collectionType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == elementType);
            var constructor = collectionType.GetConstructor(new Type[0]);

            // Fall back to List<T> if the custom type isn't working out.
            if (addMethod == null || constructor == null)
                return GetListOfTTypeInfo(elementType);

            return new ConcreteCollectionInfo(collectionType, elementType, constructor, addMethod);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfoForInterfaceType(MapTypeContext context)
        {
            // It's one of IEnumerable, ICollection or IList, so we will do some searching to find
            // an appropriate elementType and Add() method variant
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType == typeof(object))
                return GetListOfTTypeInfo(typeof(object));

            var customType = typeSettings.GetDefault().Type;

            var constructor = customType.GetConstructor(new Type[0]);
            // The custom type might have multiple .Add() method variants. We have to try to pick
            // the "Best" one, which is hard to do. Basically we want Add(<anyType>) instead of 
            // Add(object), if possible.
            var addMethods = customType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1)
                .Select(m => new { AddMethod = m, Parameter = m.GetParameters()[0] })
                .OrderBy(x => x.Parameter.ParameterType == typeof(object) ? 0 : 1)
                .ToList();

            // If we don't have a default constructor or a suitable .Add() method, fall back to
            // List<object>. There's no way to signal an error from here, so the user has to 
            // figure it out.
            if (constructor == null || addMethods.Count == 0)
                return GetListOfTTypeInfo(typeof(object));

            var bestAddMethod = addMethods.FirstOrDefault();
            return new ConcreteCollectionInfo(customType, bestAddMethod.Parameter.ParameterType, constructor, bestAddMethod.AddMethod);
        }

        private ConcreteCollectionInfo GetListOfTTypeInfo(Type elementType)
        {
            var listOfTType = typeof(List<>).MakeGenericType(elementType);
            var addTMethod = listOfTType.GetMethod(nameof(List<object>.Add), BindingFlags.Public | BindingFlags.Instance);
            var listTConstructor = listOfTType.GetConstructor(new Type[0]);
            return new ConcreteCollectionInfo(listOfTType, elementType, listTConstructor, addTMethod);
        }

        private static ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "list");
            if (context.GetExisting == null)
            {
                // if we don't have an expression to get an existing slot, just create new by
                // calling the default parameterless constructor on the collection type
                var createNewExpr = Expression.Assign(
                    newInstance,
                    Expression.New(constructor)
                );
                return new ConstructedValueExpression(new[] { createNewExpr }, newInstance, new[] { newInstance });
            }

            // Try to get the existing value. If it's null, just create new with the default
            // parameterless constructor. If it's not null we can return it.
            var getExistingExpr = Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        context.GetExisting,
                        Expression.Constant(null)
                    ),
                    Expression.Convert(
                        Expression.New(constructor),
                        context.TargetType
                    ),
                    Expression.Convert(context.GetExisting, context.TargetType)
                )
            );
            return new ConstructedValueExpression(new[] { getExistingExpr }, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression GetCollectionPopulateStatements(MapTypeContext context, Type elementType, Expression listVar, MethodInfo addMethod)
        {
            if (!context.HasColumns())
                return new ConstructedValueExpression(null);

            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // For scalar types (object is treated like a scalar here), map each column to an entry in the
            // array, consuming all remaining columns.
            if (elementType.IsSupportedPrimitiveType() || elementType == typeof(object))
            {
                var columns = context.GetColumns();
                foreach (var column in columns)
                {
                    var columnState = context.GetSubstateForColumn(column, elementType, null);
                    var result = _scalars.Compile(columnState);
                    expressions.AddRange(result.Expressions);
                    expressions.Add(
                        Expression.Call(
                            listVar,
                            addMethod,
                            result.FinalValue
                        )
                    );
                    variables.AddRange(result.Variables);
                }

                return new ConstructedValueExpression(expressions, null, variables);
            }

            // For all non-scalar types
            // loop so long as we have columns and consume columns every iteration.
            var elementState = context.ChangeTargetType(elementType);
            var numberOfColumns = context.NumberOfColumns();
            while (numberOfColumns > 0)
            {
                var result = _nonScalars.Compile(elementState);
                var newNumberOfColumns = context.NumberOfColumns();
                if (newNumberOfColumns == numberOfColumns)
                    break;
                expressions.AddRange(result.Expressions);
                expressions.Add(
                    Expression.Call(listVar, addMethod, result.FinalValue)
                );
                variables.AddRange(result.Variables);
                numberOfColumns = newNumberOfColumns;
            }
            return new ConstructedValueExpression(expressions, null, variables);
        }
    }
}