using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Attempt to instantiate a collection to fill a slot of type IList, IReadOnlyList, 
    /// ICollection, IEnumerable, etc
    /// </summary>
    public class AbstractCollectionCompiler : ICompiler
    {
        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        public AbstractCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            var elementType = GetElementType(context);
            var listType = GetConcreteListType(context, context.TargetType, elementType);

            var constructor = GetListConstructor(listType);
            var addMethod = GetListAddMethod(context.TargetType, listType, elementType);

            var originalTargetType = context.TargetType;
            context = context.ChangeTargetType(listType);
            var listVar = GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = GetCollectionPopulateStatements(context, elementType, listVar.FinalValue, addMethod);

            return new ConstructedValueExpression(
                listVar.Expressions.Concat(addStmts.Expressions),
                Expression.Convert(listVar.FinalValue, originalTargetType),
                listVar.Variables.Concat(addStmts.Variables)
            );
        }

        private static Type GetElementType(MapTypeContext context)
        {
            // It's one of IEnumerable, ICollection or IList, so we can assume the type is object
            if (context.TargetType.GenericTypeArguments.Length == 0)
                return typeof(object);

            // Otherwise it's one of IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyList<T> or
            // ICollection<T>. Get the generic type argument
            var elementType = context.TargetType.GenericTypeArguments[0];
            return elementType;
        }

        private static MethodInfo GetListAddMethod(Type targetType, Type listType, Type elementType)
        {
            return listType.GetMethod(nameof(ICollection<object>.Add)) ?? throw MapCompilerException.MissingMethod(targetType, nameof(ICollection<object>.Add), elementType);
        }

        private static ConstructorInfo GetListConstructor(Type listType)
        {
            return listType.GetConstructor(new Type[0]) ?? throw MapCompilerException.MissingParameterlessConstructor(listType);
        }

        private static Type GetConcreteListType(MapTypeContext context, Type targetType, Type elementType)
        {
            // If we have a configured type, use that. Otherwise fall back to List<T>
            var typeSettings = context.Settings.GetTypeSettings(targetType);
            if (typeSettings.BaseType != typeof(object))
            {
                var customType = typeSettings.GetDefault().Type;
                if (!typeof(ICollection<>).MakeGenericType(elementType).IsAssignableFrom(customType))
                    throw MapCompilerException.InvalidListType(customType);
                return customType;
            }

            var listType = typeof(List<>).MakeGenericType(elementType);
            if (!targetType.IsAssignableFrom(listType))
                throw MapCompilerException.CannotConvertType(targetType, listType);
            return listType;
        }

        private static ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapTypeContext context, ConstructorInfo constructor)
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

        private ConstructedValueExpression GetCollectionPopulateStatements(MapTypeContext context, Type elementType, Expression listVar, MethodInfo addMethod)
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
                    var result = _scalars.Compile(columnState);
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
                var result = _nonScalars.Compile(elementState);
                var newNumberOfColumns = context.NumberOfColumns();
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