using System;
using System.Collections.Generic;
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
            public ConcreteCollectionInfo(Type concreteType, Type elementType, MethodInfo addMethod)
            {
                ConcreteType = concreteType;
                ElementType = elementType;
                AddMethod = addMethod;
            }

            public Type ConcreteType { get; }
            public Type ElementType { get; }
            public MethodInfo AddMethod { get; }
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            // Get the concrete type to construct, the element type to populate, and the Add
            // method to call for each value
            var info = GetConcreteTypeInfo(context);
            var collectionType = info.ConcreteType;
            var elementType = info.ElementType;
            var addMethod = info.AddMethod;
            if (addMethod == null)
                throw MapCompilerException.InvalidListType(collectionType);

            // Get the default parameterless constructor
            var constructor = collectionType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(collectionType);

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

            // Start with List<T>
            var defaultCollectionType = typeof(List<>).MakeGenericType(elementType);
            var collectionType = defaultCollectionType;

            // See if we have a custom type configured which we should use instead of List<T>
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != null && typeSettings.BaseType != typeof(object))
                collectionType = typeSettings.GetDefault().Type;

            // The type should have an Add(T) method (even if the interface we're using is 
            // IReadOnly...<T>). The Add method might not be exposed through the interface.
            var addMethod = collectionType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == elementType)
                .FirstOrDefault();
            if (addMethod == null)
            {
                // The configured custom type doesn't have a suitable .Add method we can use
                // instead of freaking out, we'll fall back to List<T> so they get something
                // usable (we don't have a way to signal a warning to the user here, so they
                // have to figure it out themselves.
                var addTMethod = defaultCollectionType.GetMethod(nameof(List<object>.Add), BindingFlags.Public | BindingFlags.Instance);
                return new ConcreteCollectionInfo(defaultCollectionType, elementType, addTMethod);
            }
            return new ConcreteCollectionInfo(collectionType, elementType, addMethod);
        }

        private ConcreteCollectionInfo GetConcreteTypeInfoForInterfaceType(MapTypeContext context)
        {
            // It's one of IEnumerable, ICollection or IList, so we will do some searching to find
            // an appropriate elementType and Add() method variant
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType == typeof(object))
            {
                // We don't have any hints from the interface definition here and we don't have
                // a configured type to use which may give us some info, so just fill in with
                // List<object>.
                var listOfObjectType = typeof(List<object>);

                // List<T> only has one .Add method variant, so we don't need to double-check the
                // parameter type
                var addObjectMethod = listOfObjectType.GetMethod(nameof(List<object>.Add), BindingFlags.Public | BindingFlags.Instance);
                var objectType = typeof(object);
                return new ConcreteCollectionInfo(listOfObjectType, objectType, addObjectMethod);
            }

            var customType = typeSettings.GetDefault().Type;

            // The custom type might have multiple .Add() method variants. We have to try to pick
            // the "Best" one, which is hard to do. Basically we want Add(<anyType>) instead of 
            // Add(object), if possible.
            var addMethods = context.TargetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == nameof(ICollection<object>.Add) && m.GetParameters().Length == 1)
                .Select(m => new { AddMethod = m, Parameter = m.GetParameters()[0] })
                .OrderBy(x => x.Parameter.ParameterType == typeof(object) ? 0 : 1)
                .ToList();
            if (addMethods.Count == 0)
                throw MapCompilerException.InvalidListType(context.TargetType);
            var bestAddMethod = addMethods.FirstOrDefault();
            return new ConcreteCollectionInfo(customType, bestAddMethod.Parameter.ParameterType, bestAddMethod.AddMethod);
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