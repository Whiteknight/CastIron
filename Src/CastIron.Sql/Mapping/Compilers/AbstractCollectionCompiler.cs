using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public class AbstractCollectionCompiler : ICompiler
    {
        private readonly ICompiler _values;
        private readonly ICompiler _customObjects;

        public AbstractCollectionCompiler(ICompiler values, ICompiler customObjects)
        {
            _values = values;
            _customObjects = customObjects;
        }

        public ConstructedValueExpression Compile(MapState state)
        {
            if (state.TargetType.GenericTypeArguments.Length != 1)
                throw MapCompilerException.InvalidInterfaceType(state.TargetType);

            var elementType = state.TargetType.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            if (!state.TargetType.IsAssignableFrom(listType))
                throw MapCompilerException.CannotConvertType(state.TargetType, listType);

            var constructor = listType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(listType);

            var addMethod = listType.GetMethod(nameof(List<object>.Add));
            if (addMethod == null)
                throw MapCompilerException.MissingMethod(state.TargetType, nameof(List<object>.Add), elementType);

            var originalTargetType = state.TargetType;
            state = state.ChangeTargetType(listType);
            var listVar = GetMaybeInstantiateCollectionExpression(state, constructor);
            var addStmts = GetCollectionPopulateStatements(state, elementType, listVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                listVar.Expressions.Concat(addStmts.Expressions), 
                Expression.Convert(listVar.FinalValue, originalTargetType), 
                listVar.Variables.Concat(addStmts.Variables)
            );
        }

        private static ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapState state, ConstructorInfo constructor)
        {
            var newInstance = state.CreateVariable(state.TargetType, "list");
            if (state.GetExisting == null)
            {
                // var newInstance = new TargetType()
                var createNewExpr = Expression.Assign(newInstance, Expression.New(constructor));
                return new ConstructedValueExpression(new [] { createNewExpr }, newInstance, new [] { newInstance });
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            var getExistingExpr = Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        state.GetExisting,
                        Expression.Constant(null)
                    ),
                    Expression.Convert(
                        Expression.New(constructor),
                        state.TargetType
                    ),
                    Expression.Convert(state.GetExisting, state.TargetType)
                )
            );
            return new ConstructedValueExpression(new[] { getExistingExpr }, newInstance, new [] { newInstance });
        }

        private ConstructedValueExpression GetCollectionPopulateStatements(MapState state, Type elementType, Expression listVar, MethodInfo addMethod)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            if (elementType.IsMappableCustomObjectType())
            {
                // TODO: We should be able to loop here, and instantiate more than one instance so long as we
                // still have mappable columns (and so long as the number of mappable columns decreases each
                // iteration)
                var elementState = state.ChangeTargetType(elementType);
                var result = _customObjects.Compile(elementState);
                expressions.AddRange(result.Expressions);
                expressions.Add(
                    Expression.Call(
                        listVar, 
                        addMethod, 
                        result.FinalValue
                    )
                );
                variables.AddRange(result.Variables);
                return new ConstructedValueExpression(expressions, null, variables);
            }

            // TODO: We should be able to map to tuple types here, for Tuple<N> we should be able to map the
            // first N columns and continue in a loop

            // TODO: We should be able to map to a dictionary type here, for the first column of each name
            // map to a dictionary and loop while mappable columns exist.

            // TODO: Above TODO notes apply to ConcreteCollectionCompiler also.

            var columns = state.GetColumns();
            foreach (var column in columns)
            {
                var columnState = state.GetSubstateForColumn(column, elementType, null);
                var result = _values.Compile(columnState);
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
    }
}