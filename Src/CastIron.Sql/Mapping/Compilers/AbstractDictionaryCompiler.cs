using System;
using System.Collections.Generic;
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
        private readonly ICompiler _valueCompiler;

        public AbstractDictionaryCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            var keyType = typeof(string);
            var elementType = context.TargetType.GetGenericArguments()[1];
            var dictType = GetConcreteDictionaryType(context, elementType);

            var constructor = GetDictionaryConstructor(dictType);
            var addMethod = GetDictionaryAddMethod(context, dictType, keyType, elementType);

            var concreteState = context.ChangeTargetType(dictType);
            var dictVar = GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = AddDictionaryPopulateStatements(concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(dictVar.FinalValue, context.TargetType),
                dictVar.Variables.Concat(addStmts.Variables));
        }

        private static MethodInfo GetDictionaryAddMethod(MapTypeContext context, Type dictType, Type keyType, Type elementType)
        {
            var addMethodParameterTypes = new[] { keyType, elementType };
            return dictType.GetMethod(nameof(IDictionary<string, object>.Add), addMethodParameterTypes) ?? throw MapCompilerException.DictionaryTypeMissingAddMethod(context.TargetType);
        }

        private static ConstructorInfo GetDictionaryConstructor(Type dictType)
        {
            return dictType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.MissingParameterlessConstructor(dictType);
        }

        private static Type GetConcreteDictionaryType(MapTypeContext context, Type elementType)
        {
            var typeSettings = context.Settings.GetTypeSettings(context.TargetType);
            if (typeSettings.BaseType != typeof(object))
            {
                var customType = typeSettings.GetDefault().Type;
                if (!typeof(IDictionary<,>).MakeGenericType(typeof(string), elementType).IsAssignableFrom(customType))
                    throw MapCompilerException.InvalidDictionaryTargetType(customType);
                return customType;
            }
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!context.TargetType.IsAssignableFrom(dictType))
                throw MapCompilerException.CannotConvertType(context.TargetType, dictType);
            return dictType;
        }

        // var dict = new DictType();
        // OR
        // var dict = getExisting() == null ? new DictType() : getExisting() as DictType
        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "dict");
            if (context.GetExisting == null)
            {
                // var newInstance = new TargetType()
                var getNewExpr = Expression.Assign(
                    newInstance,
                    Expression.New(constructor)
                );
                return new ConstructedValueExpression(new[] { getNewExpr }, newInstance, new[] { newInstance });
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            var tryGetExistingExpression = Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        context.GetExisting,
                        Expression.Constant(null)
                    ),
                    Expression.Convert(
                        Expression.Assign(
                            newInstance,
                            Expression.New(constructor)
                        ),
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
                // TODO: This Substring is going to be wrong if we are more than 2 levels deep
                // Pass the column to the State to get the short versions of Original and Canonical names.
                var keyName = column.OriginalName.Substring(context.Name.Length + context.Separator.Length);
                var childName = column.CanonicalName.Substring(context.Name.Length + context.Separator.Length);
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