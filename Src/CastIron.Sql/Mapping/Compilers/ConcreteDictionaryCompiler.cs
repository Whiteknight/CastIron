using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Maps to any dictionary type which inherits string-keyed IDictionary
    /// </summary>
    public class ConcreteDictionaryCompiler : ICompiler
    {
        private readonly ICompiler _valueCompiler;

        public ConcreteDictionaryCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            var idictType = GetIDictionaryType(context);
            var elementType = idictType.GetGenericArguments()[1];

            var constructor = GetDictionaryConstructor(context.TargetType);
            var addMethod = GetIDictionaryAddMethod(context.TargetType, idictType);

            var dictVar = GetMaybeInstantiateDictionaryExpression(context, constructor);
            var addStmts = AddDictionaryPopulateStatements(context, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(dictVar.FinalValue, context.TargetType),
                dictVar.Variables.Concat(addStmts.Variables));
        }

        private static MethodInfo GetIDictionaryAddMethod(Type targetType, Type idictType)
        {
            return idictType.GetMethod(nameof(IDictionary<string, object>.Add)) ?? throw MapCompilerException.InvalidDictionaryTargetType(targetType);
        }

        private static ConstructorInfo GetDictionaryConstructor(Type targetType)
        {
            return targetType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.InvalidDictionaryTargetType(targetType);
        }

        private static Type GetIDictionaryType(MapTypeContext context)
        {
            return context.TargetType.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string))
                ?? throw MapCompilerException.InvalidDictionaryTargetType(context.TargetType);
        }

        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapTypeContext context, ConstructorInfo constructor)
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
                // TODO: This Substring is going to fail if we are more than 2 levels deep
                // We need to pass the column to the State to ask for the short versions of the Original and 
                // Canonical names.
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
