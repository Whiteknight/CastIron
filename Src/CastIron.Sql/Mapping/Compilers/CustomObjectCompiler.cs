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

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            if (!context.TargetType.IsMappableCustomObjectType())
                return ConstructedValueExpression.Nothing;

            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // Instantiate the object, using a factory or constructor
            var initExpr = AddInstantiationExpressionForObjectInstance(context);
            expressions.AddRange(initExpr.Expressions);
            variables.AddRange(initExpr.Variables);

            // Assign public writeable property values 
            var addPropertyStmts = AddPropertyAssignmentExpressions(context, initExpr.FinalValue);
            expressions.AddRange(addPropertyStmts.Expressions);
            variables.AddRange(addPropertyStmts.Variables);

            return new ConstructedValueExpression(expressions, Expression.Convert(initExpr.FinalValue, context.TargetType), variables);
        }

        private ConstructedValueExpression AddInstantiationExpressionForObjectInstance(MapTypeContext context)
        {
            var instanceVar = context.CreateVariable(context.TargetType, "instance");
            var typeInfo = context.Settings.GetTypeSettings(context.TargetType);
            var defaultSubtypeInfo = typeInfo.GetDefault();
            var defaultInstantiationExpression = AddInstantiationExpressionForSpecificType(context, defaultSubtypeInfo, instanceVar);
            var subclasses = typeInfo.GetSpecificTypes().ToList();

            // If we have no configured subclass predicates, instantiate the target type directly
            if (subclasses.Count == 0)
                return new ConstructedValueExpression(defaultInstantiationExpression.Expressions, instanceVar, defaultInstantiationExpression.Variables.Concat(new[] { instanceVar }));

            // If we have configured subclass predicates, unroll the loop and write expressions for
            // all cases. The default case will be executed if no subclass predicates match.
            var endLabel = context.CreateLabel("haveinstance");
            var expressions = new List<Expression>();
            foreach (var subclass in subclasses.Where(s => s.Predicate != null))
            {
                var instantiateExpressions = AddInstantiationExpressionForSpecificType(context, subclass, instanceVar);
                // Check the subclass predicate. If it matches, we enter into a block where we 
                // create the subclass instance and then jump to the end label. Otherwise we
                // fall through to the next subclass.
                expressions.Add(
                    Expression.IfThen(
                        Expression.Call(
                            Expression.Constant(subclass.Predicate.Target),
                            subclass.Predicate.Method,
                            context.RecordParameter
                        ),
                        Expression.Block(
                            instantiateExpressions.Variables,
                            instantiateExpressions.Expressions.Concat(new[] {
                                Expression.Goto(endLabel.Target)
                            })
                        )
                    )
                );
            }
            // A block where we fall back to the default instantiation rules for the type, and then
            // fall through to the endLabel. The block here allows us to scope variables a little
            // more tightly
            expressions.Add(
                Expression.Block(
                    defaultInstantiationExpression.Variables,
                    defaultInstantiationExpression.Expressions
                )
            );
            expressions.Add(endLabel);

            return new ConstructedValueExpression(expressions, instanceVar, new[] { instanceVar });
        }

        private ConstructedValueExpression AddInstantiationExpressionForSpecificType(MapTypeContext context, ISpecificTypeSettings specificType, ParameterExpression instanceVar)
        {
            // If we have a factory method, invoke that to get the instance, otherwise fall back 
            // to finding and calling a suitable constructor
            var factory = specificType.GetFactory();
            if (factory != null)
                return AddFactoryMethodCallExpression(context, factory, instanceVar);
            return GetConstructorCallExpression(context, specificType, instanceVar);
        }

        private static ConstructedValueExpression AddFactoryMethodCallExpression(MapTypeContext context, Func<object> factory, ParameterExpression instanceVar)
        {
            var expressions = new Expression[]
            {
                // Call the factory method and assign the result to the instance variable
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

                // check the instance variable value here to make sure it's not null. If it is null
                // we will throw an exception which is (slightly) more helpful than the default
                // NullReferenceException
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
            return new ConstructedValueExpression(expressions, instanceVar, null);
        }

        private ConstructedValueExpression GetConstructorCallExpression(MapTypeContext context, ISpecificTypeSettings specificTypeSettings, ParameterExpression instanceVar)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            var constructor = context.GetConstructor(specificTypeSettings);

            var parameters = constructor.GetParameters();
            var args = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                var substate = context.GetSubstateForProperty(name, parameter, parameter.ParameterType);

                // Compile an expression to get a value for this argument. If we don't have, it,
                // get a default expression for this type. We should have it in theory, because the
                // constructor finder shouldn't have given us a constructor which contains 
                // parameters we don't have values for.
                var expr = _values.Compile(substate);
                if (expr.FinalValue == null || expr.IsNothing)
                {
                    args[i] = parameter.ParameterType.GetDefaultValueExpression();
                    continue;
                }
                args[i] = expr.FinalValue;
                expressions.AddRange(expr.Expressions);
                variables.AddRange(expr.Variables);
            }

            // Call the constructor with the given args list
            expressions.Add(
                Expression.Assign(instanceVar,
                    Expression.New(constructor, args)
                )
            );
            return new ConstructedValueExpression(expressions, instanceVar, variables);
        }

        private ConstructedValueExpression AddPropertyAssignmentExpressions(MapTypeContext context, Expression instance)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // Get the list of public, writeable properties
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
                    if (!property.IsSettable())
                        continue;
                    MapSupportedPrimitiveType(context, instance, property, name, expressions, variables);
                    continue;
                }

                // Non-primitive types may have an existing value which can be updated in-place 
                // by reference. We pass this getPropExpr expression to the substate as the 
                // getExisting expression. If this exists, all the generated expressions will
                // update the existing value in-place.
                var getPropExpr = Expression.Property(instance, property);

                var substate = context.GetSubstateForProperty(name, property, property.PropertyType, getPropExpr);
                var expression = _values.Compile(substate);
                if (expression.Expressions == null)
                    continue;

                expressions.AddRange(expression.Expressions);
                variables.AddRange(expression.Variables);
                // If the property value is null but there is no setter, we may still populate an object 
                // but just ignore it. That seems rare but still sub-optimal.
                if (!property.IsSettable())
                    continue;

                // Set the value, if it has a setter
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

        private void MapSupportedPrimitiveType(MapTypeContext context, Expression instance, PropertyInfo property, string name, List<Expression> expressions, List<ParameterExpression> variables)
        {
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