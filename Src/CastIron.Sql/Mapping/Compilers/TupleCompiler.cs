using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiler to map columns to a tuple. Because tuple item names are not kept at runtime, the columns
    /// of the result set are mapped to the items of the tuple in order. Name matching is not used.
    /// </summary>
    public class TupleCompiler : ICompiler
    {
        private readonly ICompiler _values;

        public TupleCompiler(ICompiler values)
        {
            _values = values;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            var typeParams = context.TargetType.GenericTypeArguments;
            var factoryMethod = GetTupleFactoryMethod(context, typeParams);
            return MapTupleParameters(context, factoryMethod, typeParams);
        }

        private ConstructedValueExpression MapTupleParameters(MapTypeContext context, MethodInfo factoryMethod, Type[] typeParams)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            var args = new Expression[typeParams.Length];

            // We need to assign a value to every parameter. Loop over each parameter and map values where
            // available. When we run out of columns or have a column we cannot map, fill in a default value.
            // Where a value type may consume many columns (a custom object type, for example) it will 
            // greedily consume as much as possible before moving to the next parameter.
            for (var i = 0; i < typeParams.Length; i++)
            {
                if (!context.HasColumns())
                {
                    args[i] = typeParams[i].GetDefaultValueExpression();
                    continue;
                }

                var elementState = context.ChangeTargetType(typeParams[i]);
                var expr = _values.Compile(elementState);
                if (expr.IsNothing)
                {
                    args[i] = typeParams[i].GetDefaultValueExpression();
                    continue;
                }

                expressions.AddRange(expr.Expressions);
                variables.AddRange(expr.Variables);
                args[i] = expr.FinalValue;
            }
            return new ConstructedValueExpression(expressions, Expression.Call(null, factoryMethod, args), variables);
        }

        private static MethodInfo GetTupleFactoryMethod(MapTypeContext context, Type[] typeParams)
        {
            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .First();
            return factoryMethod;
        }
    }
}