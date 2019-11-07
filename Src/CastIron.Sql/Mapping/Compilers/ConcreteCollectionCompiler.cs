using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public class ConcreteCollectionCompiler : ICompiler
    {
        private readonly ICompiler _values;
        private readonly ICompiler _customObjects;

        public ConcreteCollectionCompiler(ICompiler values, ICompiler customObjects)
        {
            _values = values;
            _customObjects = customObjects;
        }

        public ConstructedValueExpression Compile(MapState state)
        {
            var icollectionType = state.TargetType
                .GetInterfaces()
                .FirstOrDefault(i => i.Namespace == "System.Collections.Generic" && i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (icollectionType == null)
                throw MapCompilerException.CannotConvertType(state.TargetType, typeof(ICollection<>));
            var elementType = icollectionType.GenericTypeArguments[0];

            if (!icollectionType.IsAssignableFrom(state.TargetType))
                throw MapCompilerException.CannotConvertType(state.TargetType, icollectionType);

            var constructor = state.TargetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(state.TargetType);

            var addMethod = icollectionType.GetMethod(nameof(ICollection<object>.Add));
            if (addMethod == null)
                throw MapCompilerException.MissingMethod(state.TargetType, nameof(ICollection<object>.Add), elementType);

            var listVar = GetMaybeInstantiateCollectionExpression(state, constructor);

            var addStmts = GetCollectionPopulateStatements(state, elementType, listVar.FinalValue, addMethod);

            return new ConstructedValueExpression(listVar.Expressions.Concat(addStmts.Expressions), listVar.FinalValue, listVar.Variables.Concat(addStmts.Variables));
        }

        private ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapState state, ConstructorInfo constructor)
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
                        state.TargetType),
                    Expression.Convert(state.GetExisting, state.TargetType)
                )
            );
            return new ConstructedValueExpression(new[] { getExistingExpr}, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression GetCollectionPopulateStatements(MapState state, Type elementType, Expression listVar, MethodInfo addMethod)
        {
            if (!state.HasColumns())
                return new ConstructedValueExpression(null);
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            if (elementType.IsMappableCustomObjectType())
            {
                var elementState = state.ChangeTargetType(elementType);
                var result = _customObjects.Compile(elementState);
                expressions.AddRange(result.Expressions);
                expressions.Add(Expression.Call(listVar, addMethod, result.FinalValue));
                variables.AddRange(result.Variables);
            }
            else
            {
                var columns = state.GetColumns();
                foreach (var column in columns)
                {
                    // TODO: Is it possible to try recurse here? 
                    var result = _values.Compile(state.GetSubstateForColumn(column, elementType, null));
                    expressions.AddRange(result.Expressions);
                    expressions.Add(Expression.Call(listVar, addMethod, result.FinalValue));
                    variables.AddRange(result.Variables);
                }
            }


            return new ConstructedValueExpression(expressions, null, variables);
        }
    }
}
