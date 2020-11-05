using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public static class CollectionExpressionFactory
    {
        public static ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapTypeContext context, ConstructorInfo constructor)
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

        public static ConstructedValueExpression GetCollectionPopulateStatements(ICompiler scalars, ICompiler nonScalars, MapTypeContext context, Type elementType, Expression listVar, MethodInfo addMethod)
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
                    var result = scalars.Compile(columnState);
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
                var result = nonScalars.Compile(elementState);
                var newNumberOfColumns = context.NumberOfColumns();

                // We haven't consumed any new columns, so we're done.
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