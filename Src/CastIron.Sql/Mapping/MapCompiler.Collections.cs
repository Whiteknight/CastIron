using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler
    {
        private static ConstructedValueExpression GetConcreteCollectionConversionExpression(MapCompileContext context, string name, Type targetType, Expression getExisting, ICustomAttributeProvider attrs)
        {
            var icollectionType = targetType
                .GetInterfaces()
                .FirstOrDefault(i => i.Namespace == "System.Collections.Generic" && i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (icollectionType == null)
                throw MapCompilerException.CannotConvertType(targetType, typeof(ICollection<>));
            var elementType = icollectionType.GenericTypeArguments[0];

            if (!icollectionType.IsAssignableFrom(targetType))
                throw MapCompilerException.CannotConvertType(targetType, icollectionType);

            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(targetType);

            var addMethod = icollectionType.GetMethod(nameof(ICollection<object>.Add));
            if (addMethod == null)
                throw MapCompilerException.MissingMethod(targetType, nameof(ICollection<object>.Add), elementType);

            var expressions = new List<Expression>();
            var listVar = GetMaybeInstantiateCollectionExpression(context, targetType, getExisting, constructor, expressions);

            GetCollectionPopulateStatements(context, name, attrs, elementType, listVar, addMethod, expressions);

            return new ConstructedValueExpression(expressions, listVar);
        }

        private static ConstructedValueExpression GetInterfaceCollectionConversionExpression(MapCompileContext context, string name, Type targetType, Expression getExisting, ICustomAttributeProvider attrs)
        {
            if (targetType.GenericTypeArguments.Length != 1)
                throw MapCompilerException.InvalidInterfaceType(targetType);

            var elementType = targetType.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            if (!targetType.IsAssignableFrom(listType))
                throw MapCompilerException.CannotConvertType(targetType, listType);

            var constructor = listType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw MapCompilerException.MissingParameterlessConstructor(listType);

            var addMethod = listType.GetMethod(nameof(List<object>.Add));
            if (addMethod == null)
                throw MapCompilerException.MissingMethod(targetType, nameof(List<object>.Add), elementType);

            var expressions = new List<Expression>();
            var listVar = GetMaybeInstantiateCollectionExpression(context, listType, getExisting, constructor, expressions);
            GetCollectionPopulateStatements(context, name, attrs, elementType, listVar, addMethod, expressions);

            return new ConstructedValueExpression(expressions, Expression.Convert(listVar, targetType));
        }

        private static void GetCollectionPopulateStatements(MapCompileContext context, string name, ICustomAttributeProvider attrs, Type elementType, Expression listVar, MethodInfo addMethod, List<Expression> expressions)
        {
            if (elementType.IsMappableScalarType())
            {
                GetCollectionOfScalarPopulateStatements(context, name, attrs, elementType, listVar, addMethod, expressions);
                return;
            }

            if (IsMappableCustomObjectType(elementType))
            {
                GetCollectionOfCustomObjectPopulateStatements(context, name, elementType, listVar, addMethod, expressions);
                return;
            }

            throw new MapCompilerException($"Cannot map collection with element type {elementType.Name} named '{name}'");
        }

        private static void GetCollectionOfCustomObjectPopulateStatements(MapCompileContext context, string name, Type elementType, Expression listVar, MethodInfo addMethod, List<Expression> expressions)
        {
            var subcontext = context.CreateSubcontext(elementType, name);
            var subcontextColumns = subcontext.GetColumnNameCounts();
            if (subcontextColumns.Any())
            {
                var initExprs = AddInstantiationExpressionForObjectInstance(subcontext);
                expressions.AddRange(initExprs.Expressions);
                AddPropertyAssignmentExpressions(subcontext, initExprs.FinalValue, expressions);
                expressions.Add(Expression.Call(listVar, addMethod, initExprs.FinalValue));
            }
        }

        private static void GetCollectionOfScalarPopulateStatements(MapCompileContext context, string name, ICustomAttributeProvider attrs, Type elementType, Expression listVar, MethodInfo addMethod, List<Expression> expressions)
        {
            var columns = context.GetColumnsForProperty(name, attrs);
            foreach (var column in columns)
            {
                // TODO: Is it possible to try recurse here? 
                var getScalarExpression = GetScalarMappingExpression(column.Index, context, column, elementType);
                expressions.AddRange(getScalarExpression.Expressions);
                expressions.Add(Expression.Call(listVar, addMethod, getScalarExpression.FinalValue));
                column.MarkMapped();
            }
        }

        private static ConstructedValueExpression GetArrayConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            var elementType = targetType.GetElementType();
            if (elementType == null)
                throw MapCompilerException.CannotDetermineArrayElementType(targetType);

            var constructor = targetType.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
                throw MapCompilerException.MissingArrayConstructor(targetType);

            if (elementType.IsMappableScalarType())
                return GetArrayOfPrimitiveConversionExpressions(context, name, targetType, attrs, constructor, elementType);

            if (IsMappableCustomObjectType(elementType))
                return GetArrayOfCustomObjectConversionExpression(context, name, targetType, elementType, constructor);

            throw MapCompilerException.TypeUnmappable(targetType);
        }

        private static ConstructedValueExpression GetArrayOfCustomObjectConversionExpression(MapCompileContext context, string name, Type targetType, Type elementType, ConstructorInfo constructor)
        {
            var expressions = new List<Expression>();
            var subcontext = context.CreateSubcontext(elementType, name);
            var arrayVar = subcontext.AddVariable(targetType, "array");
            expressions.Add(Expression.Assign(
                arrayVar, 
                Expression.New(
                    constructor, 
                    Expression.Constant(1))));

            var initExprs = AddInstantiationExpressionForObjectInstance(subcontext);
            expressions.AddRange(initExprs.Expressions);
            AddPropertyAssignmentExpressions(subcontext, initExprs.FinalValue, expressions);
            expressions.Add(Expression.Assign(
                Expression.ArrayAccess(
                    arrayVar, 
                    Expression.Constant(0)), 
                Expression.Convert(initExprs.FinalValue, elementType)));

            return new ConstructedValueExpression(expressions, arrayVar);
        }

        private static ConstructedValueExpression GetArrayOfPrimitiveConversionExpressions(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs, ConstructorInfo constructor, Type elementType)
        {
            var expressions = new List<Expression>();
            var columns = context.GetColumnsForProperty(name, attrs).ToList();
            var arrayVar = context.AddVariable(targetType, "array");
            expressions.Add(Expression.Assign(arrayVar, Expression.New(constructor, Expression.Constant(columns.Count))));
            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var getScalarExpression = GetScalarMappingExpression(column.Index, context, column, elementType);
                expressions.AddRange(getScalarExpression.Expressions);
                expressions.Add(Expression.Assign(Expression.ArrayAccess(arrayVar, Expression.Constant(i)), getScalarExpression.FinalValue));
                column.MarkMapped();
            }

            return new ConstructedValueExpression(expressions, arrayVar);
        }

        private static Expression GetMaybeInstantiateCollectionExpression(MapCompileContext context, Type targetType, Expression getExisting, ConstructorInfo constructor, List<Expression> expressions)
        {
            var newInstance = context.AddVariable(targetType, "list");
            if (getExisting == null)
            {
                // var newInstance = new TargetType()
                expressions.Add(Expression.Assign(newInstance, Expression.New(constructor)));
                return newInstance;
            }

            // var newInstance = getExisting() == null ? new TargetType() : getExisting()
            expressions.Add(Expression.Assign(
                newInstance,
                Expression.Condition(
                    Expression.Equal(
                        getExisting,
                        Expression.Constant(null)),
                    Expression.Convert(
                            Expression.New(constructor), 
                        targetType),
                    Expression.Convert(getExisting, targetType))));
            return newInstance;
        }
    }
}
