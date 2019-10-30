using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public partial class MapCompiler
    {
        private static bool IsMappableCustomObjectType(Type t)
        {
            return t.IsClass
                   && !t.IsInterface
                   && !t.IsAbstract
                   && !t.IsArray
                   && t != typeof(string)
                   && !(t.Namespace ?? string.Empty).StartsWith("System.Collections");
        }

        // var instance = ...
        private static ConstructedValueExpression AddInstantiationExpressionForObjectInstance(MapCompileContext context)
        {
            if (context.CreatePreferences.Factory != null)
                return AddFactoryMethodCallExpression(context.CreatePreferences.Factory, context);
            return GetConstructorCallExpression(context);
        }

        private static readonly MethodInfo _createMapExceptionMethod = typeof(DataMappingException).GetMethod(nameof(DataMappingException.UserFactoryReturnedNull), BindingFlags.Public | BindingFlags.Static);

        // var instance = cast(type, factory());
        // if (instance == null) throw DataMappingException.UserFactoryReturnedNull(type);
        private static ConstructedValueExpression AddFactoryMethodCallExpression<T>(Func<T> factory, MapCompileContext context)
        {
            var instanceVar = context.AddVariable(context.Specific, "instance");
            var expressions = new Expression[]
            {
                // instance = cast(type, factory());
                Expression.Assign(
                    instanceVar,
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(factory.Target),
                            factory.Method
                        ),
                        context.Specific
                    )
                ),

                // if (instance == null) throw DataMappingException.UserFactoryReturnedNull(type);
                Expression.IfThen(
                    Expression.Equal(instanceVar, Expression.Constant(null)),
                    Expression.Throw(
                        Expression.Call(_createMapExceptionMethod, Expression.Constant(context.Specific))
                    )
                )
            };
            return new ConstructedValueExpression(expressions, instanceVar);
        }

        // var instance = new MyType(converted args)
        private static ConstructedValueExpression GetConstructorCallExpression(MapCompileContext context)
        {
            var instanceVar = context.AddVariable(context.Specific, "instance");
            var expressions = new List<Expression>();
            var constructor = context.GetConstructor();
            var parameters = constructor.GetParameters();

            // Map columns to constructor parameters by name, marking the columns consumed so they aren't used again later
            var args = parameters.Select(p => GetConstructorArgumentExpression(context, p, expressions));
            expressions.Add(
                Expression.Assign(instanceVar,
                    Expression.New(constructor, args)
                )
            );
            return new ConstructedValueExpression(expressions, instanceVar);
        }

        // argument = convert(column to parameterType)
        private static Expression GetConstructorArgumentExpression(MapCompileContext context, ParameterInfo parameter, List<Expression> expressions)
        {
            var name = parameter.Name.ToLowerInvariant();
            if (!context.Columns.HasColumn(name) && !parameter.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                return parameter.ParameterType.GetDefaultValueExpression();

            var expr = GetConversionExpression(context, parameter.Name.ToLowerInvariant(), parameter.ParameterType, null, parameter);
            if (expr.FinalValue == null)
                return parameter.ParameterType.GetDefaultValueExpression();

            expressions.AddRange(expr.Expressions);
            return expr.FinalValue;
        }

        // var instance = new MyType(converted columns) { Prop = converted column, ... }
        private static ConstructedValueExpression GetCustomObjectConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var contextToUse = name == null ? context : context.CreateSubcontext(targetType, name);
            var expressions = new List<Expression>();
            var initExpr = AddInstantiationExpressionForObjectInstance(contextToUse);
            expressions.AddRange(initExpr.Expressions);
            AddPropertyAssignmentExpressions(contextToUse, initExpr.FinalValue, expressions);

            return new ConstructedValueExpression(expressions, Expression.Convert(initExpr.FinalValue, targetType));
        }

        // object instance = ...
        private static ConstructedValueExpression GetObjectConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            // At the top-level (name==null) we convert to dictionary to preserve column name information
            if (name == null)
            {
                var dictExpr = GetConcreteDictionaryConversionExpression(context, null, typeof(Dictionary<string, object>), null);
                return new ConstructedValueExpression(dictExpr.Expressions, Expression.Convert(dictExpr.FinalValue, typeof(object)));
            }

            // At the nested level (name!=null) we convert to a scalar (n==1) or an array (n>1) because name is always the same
            var numColumns = context.Columns.GetColumns(name).Count();
            if (numColumns == 0)
                return new ConstructedValueExpression(Expression.Convert(Expression.Constant(null), typeof(object)));
            if (numColumns == 1)
            {
                var firstColumn = context.Columns.GetColumn(name);
                firstColumn.MarkMapped();
                return GetScalarMappingExpression(firstColumn.Index, context, firstColumn, targetType);
            }

            var exprs = GetArrayConversionExpression(context, name, typeof(object[]), attrs);
            return new ConstructedValueExpression(exprs.Expressions, Expression.Convert(exprs.FinalValue, typeof(object)));
        }

        // property = convert(column)
        private static void AddPropertyAssignmentExpressions(MapCompileContext context, Expression instance, List<Expression> expressions)
        {
            var properties = context.Specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod != null)
                .Where(p => p.CanRead)
                .Where(p => !p.GetMethod.IsPrivate);
            foreach (var property in properties)
            {
                // Primitive types cannot be updated in place, they must have a public setter
                if (property.PropertyType.IsSupportedPrimitiveType())
                {
                    if (!property.IsSettable())
                        continue;
                    var primitiveValueExpression = GetConversionExpression(context, property.Name.ToLowerInvariant(), property.PropertyType, null, property);
                    if (primitiveValueExpression.FinalValue == null)
                        continue;
                    expressions.AddRange(primitiveValueExpression.Expressions);
                    expressions.Add(Expression.Call(instance, property.GetSetMethod(), Expression.Convert(primitiveValueExpression.FinalValue, property.PropertyType)));
                    continue;
                }

                // Non-primitive types may have an existing value which can be updated in-place
                var getPropExpr = Expression.Property(instance, property);
                var expression = GetConversionExpression(context, property.Name.ToLowerInvariant(), property.PropertyType, getPropExpr, property);
                if (expression == null)
                    continue;

                expressions.AddRange(expression.Expressions);
                // TODO: If the property value is null but there is no setter, we may still populate an object 
                // but just ignore it. That seems rare but still sub-optimal.
                if (property.IsSettable())
                    expressions.Add(Expression.Call(instance, property.GetSetMethod(), Expression.Convert(expression.FinalValue, property.PropertyType)));
            }
        }
    }
}
