using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
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

            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!state.TargetType.IsAssignableFrom(dictType))
                throw MapCompilerException.CannotConvertType(state.TargetType, dictType);

            var constructor = dictType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(dictType);

            var addMethod = dictType.GetMethod(nameof(Dictionary<string, object>.Add));
            if (addMethod == null)
                throw MapCompilerException.DictionaryTypeMissingAddMethod(state.TargetType);

            var concreteState = state.ChangeTargetType(dictType);
            var dictVar = GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = AddDictionaryPopulateStatements(concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions), 
                Expression.Convert(dictVar.FinalValue, state.TargetType), 
                dictVar.Variables.Concat(addStmts.Variables));
        }

        // var dict = new DictType();
        // OR
        // var dict = getExisting() == null ? new DictType() : getExisting() as DictType
        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapState state, ConstructorInfo constructor)
        {
            var expressions = new List<Expression>();
            var newInstance = state.CreateVariable(state.TargetType, "dict");
            if (state.GetExisting == null)
            {
                // var newInstance = new TargetType()
                expressions.Add(
                    Expression.Assign(
                        newInstance,
                        Expression.New(constructor)
                    )
                );
                return new ConstructedValueExpression(expressions, newInstance, new[] { newInstance });
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            expressions.Add(Expression.Assign(
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
                )
            );
            return new ConstructedValueExpression(expressions, newInstance, new[] { newInstance });
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
                var keyName = column.OriginalName.Substring(state.Name.Length + 1);
                var childName = column.CanonicalName.Substring(state.Name.Length + 1);
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