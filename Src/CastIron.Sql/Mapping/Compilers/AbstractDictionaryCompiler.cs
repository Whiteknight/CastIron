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

        public ConstructedValueExpression Compile(MapState state)
        {
            var elementType = state.TargetType.GetGenericArguments()[1];

            var dictType = GetMatchingDictionaryType(state, elementType);

            var constructor = GetDictionaryConstructor(dictType);

            var addMethod = GetDictionaryAddMethod(state, dictType);

            var concreteState = state.ChangeTargetType(dictType);
            var dictVar = GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = AddDictionaryPopulateStatements(concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions), 
                Expression.Convert(dictVar.FinalValue, state.TargetType), 
                dictVar.Variables.Concat(addStmts.Variables));
        }

        private static MethodInfo GetDictionaryAddMethod(MapState state, Type dictType)
        {
            return dictType.GetMethod(nameof(Dictionary<string, object>.Add)) ?? throw MapCompilerException.DictionaryTypeMissingAddMethod(state.TargetType);
        }

        private static ConstructorInfo GetDictionaryConstructor(Type dictType)
        {
            return dictType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.MissingParameterlessConstructor(dictType);
        }

        private static Type GetMatchingDictionaryType(MapState state, Type elementType)
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!state.TargetType.IsAssignableFrom(dictType))
                throw MapCompilerException.CannotConvertType(state.TargetType, dictType);
            return dictType;
        }

        // var dict = new DictType();
        // OR
        // var dict = getExisting() == null ? new DictType() : getExisting() as DictType
        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapState state, ConstructorInfo constructor)
        {
            
            var newInstance = state.CreateVariable(state.TargetType, "dict");
            if (state.GetExisting == null)
            {
                // var newInstance = new TargetType()
                var getNewExpr = Expression.Assign(
                    newInstance,
                    Expression.New(constructor)
                );
                return new ConstructedValueExpression(new [] { getNewExpr }, newInstance, new[] { newInstance });
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            var tryGetExistingExpression = Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        state.GetExisting,
                        Expression.Constant(null)
                    ),
                    Expression.Convert(
                        Expression.Assign(
                            newInstance,
                            Expression.New(constructor)
                        ),
                        state.TargetType
                    ),
                    Expression.Convert(state.GetExisting, state.TargetType)
                )
            );
            return new ConstructedValueExpression(new [] { tryGetExistingExpression }, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression AddDictionaryPopulateStatements(MapState state, Type elementType, Expression dictVar, MethodInfo addMethod)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            if (state.Name == null)
            {
                var firstColumnsByName = state.GetFirstIndexForEachColumnName();
                foreach (var column in firstColumnsByName)
                {
                    var substate = state.GetSubstateForProperty(column.CanonicalName, null, elementType);
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

            var columns = state.GetFirstIndexForEachColumnName();
            foreach (var column in columns)
            {
                // TODO: This Substring is going to be wrong if we are more than 2 levels deep
                // Pass the column to the State to get the short versions of Original and Canonical names.
                var keyName = column.OriginalName.Substring(state.Name.Length + state.Separator.Length);
                var childName = column.CanonicalName.Substring(state.Name.Length + state.Separator.Length);
                var columnSubstate = state.GetSubstateForColumn(column, elementType, childName);
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