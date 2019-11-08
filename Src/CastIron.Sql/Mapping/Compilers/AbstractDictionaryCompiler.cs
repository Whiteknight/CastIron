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

        public ConstructedValueExpression Compile(MapContext context)
        {
            var elementType = context.TargetType.GetGenericArguments()[1];
            var dictType = GetMatchingDictionaryType(context, elementType);

            var constructor = GetDictionaryConstructor(dictType);
            var addMethod = GetDictionaryAddMethod(context, dictType);

            var concreteState = context.ChangeTargetType(dictType);
            var dictVar = GetMaybeInstantiateDictionaryExpression(concreteState, constructor);
            var addStmts = AddDictionaryPopulateStatements(concreteState, elementType, dictVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                dictVar.Expressions.Concat(addStmts.Expressions), 
                Expression.Convert(dictVar.FinalValue, context.TargetType), 
                dictVar.Variables.Concat(addStmts.Variables));
        }

        private static MethodInfo GetDictionaryAddMethod(MapContext context, Type dictType)
        {
            return dictType.GetMethod(nameof(Dictionary<string, object>.Add)) ?? throw MapCompilerException.DictionaryTypeMissingAddMethod(context.TargetType);
        }

        private static ConstructorInfo GetDictionaryConstructor(Type dictType)
        {
            // TODO: If it's a custom type, we should be able to call IConstructorFinder and try to get a 
            // mappable constructor
            return dictType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.MissingParameterlessConstructor(dictType);
        }

        private static Type GetMatchingDictionaryType(MapContext context, Type elementType)
        {
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!context.TargetType.IsAssignableFrom(dictType))
                throw MapCompilerException.CannotConvertType(context.TargetType, dictType);
            return dictType;
        }

        // var dict = new DictType();
        // OR
        // var dict = getExisting() == null ? new DictType() : getExisting() as DictType
        private static ConstructedValueExpression GetMaybeInstantiateDictionaryExpression(MapContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "dict");
            if (context.GetExisting == null)
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
            return new ConstructedValueExpression(new [] { tryGetExistingExpression }, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression AddDictionaryPopulateStatements(MapContext context, Type elementType, Expression dictVar, MethodInfo addMethod)
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