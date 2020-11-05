using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public static class DictionaryExpressionFactory
    {
        public static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "dictionary");
            if (context.GetExisting == null)
            {
                // There's no way to get an existing value, so call the parameterless constructor

                var getNewInstanceExpr = Expression.Assign(
                    newInstance,
                    Expression.New(constructor)
                );
                return new ConstructedValueExpression(new[] { getNewInstanceExpr }, newInstance, new[] { newInstance });
            }

            // Try to get the existing value. If we have it return it. Otherwise call the default
            // parameterless constructor
            var tryGetExistingInstanceExpr = Expression.Assign(
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
            return new ConstructedValueExpression(new[] { tryGetExistingInstanceExpr }, newInstance, new[] { newInstance });
        }

        public static ConstructedValueExpression AddDictionaryPopulateStatements(ICompiler values, MapTypeContext context, Type elementType, Expression dictVar, MethodInfo addMethod)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            if (context.Name == null)
            {
                var firstColumnsByName = context.GetFirstIndexForEachColumnName();
                foreach (var column in firstColumnsByName)
                {
                    var substate = context.GetSubstateForProperty(column.CanonicalName, null, elementType);
                    var getScalarExpression = values.Compile(substate);
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
                var getScalarExpression = values.Compile(columnSubstate);
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
