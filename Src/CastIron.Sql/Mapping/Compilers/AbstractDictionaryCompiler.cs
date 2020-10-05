using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Maps abstract types IDictionary and IReadOnlyDictionary to a concrete Dictionary class with string
    /// key
    /// </summary>
    public class AbstractDictionaryCompiler : ICompiler
    {
        private const string AddMethodName = nameof(IDictionary<string, object>.Add);

        private readonly ICompiler _valueCompiler;

        public AbstractDictionaryCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        private struct ConcreteTypeInfo
        {
            public ConcreteTypeInfo(Type concreteType, ConstructorInfo constructor, MethodInfo addMethod)
            {
                ConcreteType = concreteType;
                Constructor = constructor;
                AddMethod = addMethod;
            }

            public Type ConcreteType { get; }
            public ConstructorInfo Constructor { get; }
            public MethodInfo AddMethod { get; }
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!IsSupportedType(context.TargetType))
                return ConstructedValueExpression.Nothing;

            var genericArguments = context.TargetType.GetGenericArguments();
            Debug.Assert(genericArguments.Length == 2);

            var keyType = typeof(string);
            Debug.Assert(genericArguments[0] == typeof(string));

            var elementType = context.TargetType.GetGenericArguments()[1];
            Debug.Assert(elementType != null);

            var typeInfo = GetConcreteTypeInfo(context, keyType, elementType);
            var dictType = typeInfo.ConcreteType;
            Debug.Assert(dictType != null);

            var constructor = typeInfo.Constructor;
            Debug.Assert(constructor != null);

            var addMethod = typeInfo.AddMethod;
            Debug.Assert(addMethod != null);

            var concreteState = context.ChangeTargetType(dictType);
            var dictVar = GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = AddDictionaryPopulateStatements(concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(dictVar.FinalValue, context.TargetType),
                dictVar.Variables.Concat(addStmts.Variables)
            );
        }

        // Is one of IDictionary<string,X> or IReadOnlyDictionary<string,X>
        private static bool IsSupportedType(Type t)
        {
            if (!t.IsGenericType || !t.IsInterface)
                return false;
            var genericDef = t.GetGenericTypeDefinition();
            return (genericDef == typeof(IDictionary<,>) || genericDef == typeof(IReadOnlyDictionary<,>))
                   && t.GenericTypeArguments[0] == typeof(string);
        }

        private ConcreteTypeInfo GetConcreteTypeInfo(MapTypeContext context, Type keyType, Type elementType)
        {
            // If we have a custom type try to use that. If we don't have one, or it fails any
            // of the precondition checks, fall back to Dictionary<string, T>
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != typeof(object))
            {
                var customType = typeSettings.GetDefault().Type;
                var constructor = customType.GetConstructor(Type.EmptyTypes);
                var addMethod = customType.GetMethod(AddMethodName, BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType, elementType }, null);
                if (constructor == null || addMethod == null)
                    return GetDictionaryOfTTypeInfo(keyType, elementType);
                return new ConcreteTypeInfo(customType, constructor, addMethod);
            }
            return GetDictionaryOfTTypeInfo(keyType, elementType);
        }

        private ConcreteTypeInfo GetDictionaryOfTTypeInfo(Type keyType, Type elementType)
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            var constructor = dictType.GetConstructor(Type.EmptyTypes);
            var addMethod = dictType.GetMethod(AddMethodName, new[] { keyType, elementType });
            return new ConcreteTypeInfo(dictType, constructor, addMethod);
        }

        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "dictionary");
            if (context.GetExisting == null)
            {
                // If we don't have a way to get an existing dictionary, just create new with the
                // default parameterless constructor
                var getNewExpr = Expression.Assign(
                    newInstance,
                    Expression.New(constructor)
                );
                return new ConstructedValueExpression(new[] { getNewExpr }, newInstance, new[] { newInstance });
            }

            // Try to get an existing value. If we have it, return that directly. Otherwise create
            // a new one with the default parameterless constructor
            var tryGetExistingExpression = Expression.Assign(
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
            return new ConstructedValueExpression(new[] { tryGetExistingExpression }, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression AddDictionaryPopulateStatements(MapTypeContext context, Type elementType, Expression dictVar, MethodInfo addMethod)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            if (context.Name == null)
            {
                var firstColumnsByName = context.GetFirstIndexForEachColumnName();
                foreach (var column in firstColumnsByName)
                {
                    var substate = context.GetSubstateForProperty(column.CanonicalName, null, elementType);
                    var getScalarExpression = _valueCompiler.Compile(substate);
                    expressions.AddRange(getScalarExpression.Expressions);
                    expressions.Add(
                        Expression.Call(
                            dictVar,
                            addMethod,
                            Expression.Constant(column.OriginalName),
                            getScalarExpression.FinalValue
                        )
                    );
                    variables.AddRange(getScalarExpression.Variables);
                }

                return new ConstructedValueExpression(expressions, null, variables);
            }

            var columns = context.GetFirstIndexForEachColumnName();
            foreach (var column in columns)
            {
                var keyName = column.OriginalName.Substring(context.CurrentPrefix.Length);
                var childName = column.CanonicalName.Substring(context.CurrentPrefix.Length);
                var columnSubstate = context.GetSubstateForColumn(column, elementType, childName);
                var getScalarExpression = _valueCompiler.Compile(columnSubstate);
                expressions.AddRange(getScalarExpression.Expressions);
                expressions.Add(
                    Expression.Call(
                        dictVar,
                        addMethod,
                        Expression.Constant(keyName),
                        getScalarExpression.FinalValue
                    )
                );
                variables.AddRange(getScalarExpression.Variables);
            }

            return new ConstructedValueExpression(expressions, null, variables);
        }
    }
}