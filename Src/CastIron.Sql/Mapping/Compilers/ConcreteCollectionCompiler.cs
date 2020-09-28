using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiles a mapping for any concrete implementation of ICollection
    /// </summary>
    public class ConcreteCollectionCompiler : ICompiler
    {
        private readonly ICompiler _scalars;
        private readonly ICompiler _nonScalars;

        public ConcreteCollectionCompiler(ICompiler scalars, ICompiler nonScalars)
        {
            _scalars = scalars;
            _nonScalars = nonScalars;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            var iCollectionType = GetICollectionInterfaceType(context);
            var elementType = iCollectionType.GenericTypeArguments[0];

            var constructor = GetConstructor(context);
            var addMethod = GetAddMethod(context, iCollectionType, elementType);

            var listVar = GetMaybeInstantiateCollectionExpression(context, constructor);
            var addStmts = GetCollectionPopulateStatements(context, elementType, listVar.FinalValue, addMethod);

            return new ConstructedValueExpression(listVar.Expressions.Concat(addStmts.Expressions), listVar.FinalValue, listVar.Variables.Concat(addStmts.Variables));
        }

        private static MethodInfo GetAddMethod(MapTypeContext context, Type icollectionType, Type elementType)
        {
            return icollectionType.GetMethod(nameof(ICollection<object>.Add)) ?? throw MapCompilerException.MissingMethod(context.TargetType, nameof(ICollection<object>.Add), elementType);
        }

        private static ConstructorInfo GetConstructor(MapTypeContext context)
        {
            return context.TargetType.GetConstructor(Type.EmptyTypes) ?? throw MapCompilerException.MissingParameterlessConstructor(context.TargetType);
        }

        private static Type GetICollectionInterfaceType(MapTypeContext context)
        {
            var icollectionType = context.TargetType
                .GetInterfaces()
                .FirstOrDefault(i => i.Namespace == "System.Collections.Generic" && i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                ?? throw MapCompilerException.CannotConvertType(context.TargetType, typeof(ICollection<>));
            if (!icollectionType.IsAssignableFrom(context.TargetType))
                throw MapCompilerException.CannotConvertType(context.TargetType, icollectionType);
            return icollectionType;
        }

        private ConstructedValueExpression GetMaybeInstantiateCollectionExpression(MapTypeContext context, ConstructorInfo constructor)
        {
            var newInstance = context.CreateVariable(context.TargetType, "list");
            if (context.GetExisting == null)
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
            return new ConstructedValueExpression(new[] { getExistingExpr}, newInstance, new[] { newInstance });
        }

        private ConstructedValueExpression GetCollectionPopulateStatements(MapTypeContext context, Type elementType, Expression listVar, MethodInfo addMethod)
        {
            if (!context.HasColumns())
                return new ConstructedValueExpression(null);

            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // For primitives or "object" loop over all remaining columns, converting one column into
            // one entry in the array
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
                expressions.Add(Expression.Call(listVar, addMethod, result.FinalValue));
                variables.AddRange(result.Variables);
                numberOfColumns = newNumberOfColumns;
            }
            return new ConstructedValueExpression(expressions, null, variables);
        }
    }
}
