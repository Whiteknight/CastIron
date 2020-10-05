using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiles a mapping for any concrete implementation of ICollection
    /// </summary>
    public class ConcreteCollectionCompiler : ICompiler
    {
        private const string AddMethodName = nameof(ICollection<object>.Add);

        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        public ConcreteCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        private struct ConcreteTypeInfo
        {
            public ConcreteTypeInfo(Type concreteType, Type elementType, ConstructorInfo constructor, MethodInfo addMethod)
            {
                ConcreteType = concreteType;
                ElementType = elementType;
                Constructor = constructor;
                AddMethod = addMethod;
            }

            public Type ConcreteType { get; }
            public Type ElementType { get; }
            public ConstructorInfo Constructor { get; }
            public MethodInfo AddMethod { get; }
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            // We have a concrete type which implements IEnumerable/IEnumerable<T> and has an Add 
            // method. Get these things.
            var info = GetTypeInfo(context);
            var collectionType = info.ConcreteType;
            Debug.Assert(collectionType != null);

            var elementType = info.ElementType;
            Debug.Assert(elementType != null);

            var constructor = info.Constructor;
            Debug.Assert(constructor != null);

            var addMethod = info.AddMethod;
            Debug.Assert(addMethod != null);

            var listVar = GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = GetCollectionPopulateStatements(context, elementType, listVar.FinalValue, addMethod);

            return new ConstructedValueExpression(listVar.Expressions.Concat(addStmts.Expressions), listVar.FinalValue, listVar.Variables.Concat(addStmts.Variables));
        }

        private static bool IsSupportedType(Type t)
            =>
                !t.IsInterface
                && !t.IsAbstract
                && t.GetInterfaces().Any(i => i == typeof(IEnumerable) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                && t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(m => m.Name == "Add");

        private ConcreteTypeInfo GetTypeInfo(MapTypeContext context)
        {
            var targetType = context.TargetType;
            var typeSettings = context.Settings.GetTypeSettings(targetType);
            if (typeSettings.BaseType != typeof(object))
                targetType = typeSettings.GetDefault().Type;

            // We absolutely need a constructor here, there's no way around it. The user has asked
            // for a specific concrete type, we can either return that type or the configured 
            // subclass, but we can't default to something else with a parameterless constructor
            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(targetType);

            // Same with .Add(). The type absolutely must have a .Add(T) method variant or we
            // cannot proceed
            var addMethods = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == AddMethodName && m.GetParameters().Length == 1)
                .ToList();
            if (addMethods.Count == 0)
                throw MapCompilerException.MissingMethod(targetType, AddMethodName);

            // Find all IEnumerable<T> interfaces. For each one we're going to look for a matching
            // .Add(T) method. If we find a match, that's what we're going with.
            var iEnumerableOfTTypes = targetType
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .ToDictionary(t => t.GetGenericArguments()[0]);
            if (iEnumerableOfTTypes.Count > 0)
            {
                var bestAddMethod = addMethods.FirstOrDefault(m => iEnumerableOfTTypes.ContainsKey(m.GetParameters()[0].ParameterType));
                if (bestAddMethod != null)
                {
                    var bestAddElementType = bestAddMethod.GetParameters()[0].ParameterType;
                    return new ConcreteTypeInfo(targetType, bestAddElementType, constructor, bestAddMethod);
                }
            }

            // At this point we know the collection must be IEnumerable because of precondition
            // checks. So we'll just use any available Add method 
            var addMethod = addMethods.FirstOrDefault();
            var elementType = addMethod.GetParameters()[0].ParameterType;
            return new ConcreteTypeInfo(targetType, elementType, constructor, addMethod);
        }

        private ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "list");
            if (context.GetExisting == null)
            {
                // call constructor because we don't have an accessor: var newInstance = new TargetType()
                var createNewExpr = Expression.Assign(newInstance, Expression.New(constructor));
                return new ConstructedValueExpression(new[] { createNewExpr }, newInstance, new[] { newInstance });
            }

            // create new if we don't have an existing value to fill in
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

            // For primitives or "object" loop over all remaining columns, converting one column into
            // one entry in the array
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
                expressions.Add(Expression.Call(listVar, addMethod, result.FinalValue));
                variables.AddRange(result.Variables);
                numberOfColumns = newNumberOfColumns;
            }
            return new ConstructedValueExpression(expressions, null, variables);
        }
    }
}
