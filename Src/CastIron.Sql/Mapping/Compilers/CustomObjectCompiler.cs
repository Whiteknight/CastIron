using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Maps any class except one of the other supported options
    /// </summary>
    public class CustomObjectCompiler : ICompiler
    {
        private static readonly MethodInfo _createMapExceptionMethod = typeof(DataMappingException).GetMethod(nameof(DataMappingException.UserFactoryReturnedNull), BindingFlags.Public | BindingFlags.Static);

        private readonly ICompiler _values;
        private readonly ICompiler _scalars;

        public CustomObjectCompiler(ICompiler values, ICompiler scalars)
        {
            _values = values;
            _scalars = scalars;
        }

        public ConstructedValueExpression Compile(MapContext context)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            var initExpr = AddInstantiationExpressionForObjectInstance(context);
            expressions.AddRange(initExpr.Expressions);
            variables.AddRange(initExpr.Variables);

            var addPropertyStmts = AddPropertyAssignmentExpressions(context, initExpr.FinalValue);
            expressions.AddRange(addPropertyStmts.Expressions);
            variables.AddRange(addPropertyStmts.Variables);

            return new ConstructedValueExpression(expressions, Expression.Convert(initExpr.FinalValue, context.TargetType), variables);
        }

        private ConstructedValueExpression AddInstantiationExpressionForObjectInstance(MapContext context)
        {
            var factory = context.GetFactoryForCurrentObjectType();
            return factory != null ? AddFactoryMethodCallExpression(context, factory) : GetConstructorCallExpression(context);
        }

        // var instance = cast(type, factory());
        // if (instance == null) throw DataMappingException.UserFactoryReturnedNull(type);
        private static ConstructedValueExpression AddFactoryMethodCallExpression(MapContext context, Func<object> factory)
        {
            var instanceVar = context.CreateVariable(context.TargetType, "instance");
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
                        context.TargetType
                    )
                ),

                // if (instance == null) throw DataMappingException.UserFactoryReturnedNull(type);
                Expression.IfThen(
                    Expression.Equal(
                        instanceVar, 
                        Expression.Constant(null)
                    ),
                    Expression.Throw(
                        Expression.Call(
                            _createMapExceptionMethod, 
                            Expression.Constant(context.TargetType)
                        )
                    )
                )
            };
            return new ConstructedValueExpression(expressions, instanceVar, new [] { instanceVar });
        }

        // var instance = new MyType(converted args)
        private ConstructedValueExpression GetConstructorCallExpression(MapContext context)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            var instanceVar = context.CreateVariable(context.TargetType, "instance");
            variables.Add(instanceVar);
            var constructor = context.GetConstructorForCurrentObjectType();
            var parameters = constructor.GetParameters();
            var args = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                var substate = context.GetSubstateForProperty(name, parameter, parameter.ParameterType);
                var expr = _values.Compile(substate);
                args[i] = expr.FinalValue ?? parameter.ParameterType.GetDefaultValueExpression();
                if (!expr.IsNothing)
                {
                    expressions.AddRange(expr.Expressions);
                    variables.AddRange(expr.Variables);
                    continue;
                }

                args[i] = parameter.ParameterType.GetDefaultValueExpression();
            }

            expressions.Add(
                Expression.Assign(instanceVar,
                    Expression.New(constructor, args)
                )
            );
            return new ConstructedValueExpression(expressions, instanceVar, variables);
        }

        // property = convert(column)
        private ConstructedValueExpression AddPropertyAssignmentExpressions(MapContext context, Expression instance)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            var properties = context.TargetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod != null)
                .Where(p => p.CanRead)
                .Where(p => !p.GetMethod.IsPrivate);

            foreach (var property in properties)
            {
                var name = property.Name.ToLowerInvariant();
                // Primitive types cannot be updated in place, they must have a public setter
                if (property.PropertyType.IsSupportedPrimitiveType())
                {
                    MapSupportedPrimitiveType(context, instance, property, name, expressions, variables);
                    continue;
                }

                // Non-primitive types may have an existing value which can be updated in-place
                var getPropExpr = Expression.Property(instance, property);

                var substate = context.GetSubstateForProperty(name, property, property.PropertyType, getPropExpr);
                var expression = _values.Compile(substate);
                if (expression.Expressions == null)
                    continue;

                expressions.AddRange(expression.Expressions);
                variables.AddRange(expression.Variables);
                // TODO: If the property value is null but there is no setter, we may still populate an object 
                // but just ignore it. That seems rare but still sub-optimal.
                if (!property.IsSettable())
                    continue;
                
                expressions.Add(
                    Expression.Call(
                        instance, 
                        property.GetSetMethod(), 
                        Expression.Convert(
                            expression.FinalValue, 
                            property.PropertyType
                        )
                    )
                );
            }

            return new ConstructedValueExpression(expressions, null, variables);
        }

        private void MapSupportedPrimitiveType(MapContext context, Expression instance, PropertyInfo property, string name, List<Expression> expressions, List<ParameterExpression> variables)
        {
            if (!property.IsSettable())
                return;
            var propertyState = context.GetSubstateForProperty(name, property, property.PropertyType);
            var primitiveValueExpression = _scalars.Compile(propertyState);
            if (primitiveValueExpression.FinalValue == null)
                return;
            expressions.AddRange(primitiveValueExpression.Expressions);
            expressions.Add(
                Expression.Call(
                    instance,
                    property.GetSetMethod(),
                    Expression.Convert(
                        primitiveValueExpression.FinalValue,
                        property.PropertyType
                    )
                )
            );
            variables.AddRange(primitiveValueExpression.Variables);
        }
    }
}